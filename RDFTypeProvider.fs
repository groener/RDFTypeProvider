namespace Samples.RdfTypeProvider

open System
open System.Reflection
open System.Collections.Generic
open System.IO
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open System.Text.RegularExpressions
    


[<TypeProvider>]
type RDFTypeProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()
    let ns = "Samples.RdfTypeProvider"
    let asm = Assembly.GetExecutingAssembly()

    let createTypes(schemaUrl, numOfIndividuals, schemaSampleAmount, rootTypeName) = 
        let connector = new Connector(schemaUrl)
        let serviceType = ProvidedTypeDefinition("RdfService",baseType=Some typeof<Connector>,HideObjectMethods=true)        
       
        let getRdfTypes =
            // FIRST : get explicit types
            let rdfTypesExplicit = connector.getExplicitTypes()
            // SECOND: now we extend types from domains
            let explicitRDFProperties = connector.getExplicitProperties()
            let explicitRDFObjectProperties = connector.getExplicitObjectProperties()
            let rdfTypesOfDomain =  List.concat [ for property in explicitRDFProperties ->  connector.getDomainTypes(property) ]
            // Combine types:  Explicit, Domain (and also Range types)
            rdfTypesExplicit  @ rdfTypesOfDomain |> Seq.distinctBy id
        

        /// Core type dictionary.  Name * ProvidedType.  E.g.,  http://namespace/Actor (a class) or and specific individuals
        let dictClassTypes = Dictionary<string, ProvidedTypeDefinition>(HashIdentity.Structural)

        /// Holds "Collection" and "Individual" types that sit on the root level of the type system, e.g., ActorCollection
        // Collection provides access to the Individual type, which in turn holds a property for each unique individual type.
        // In the future the Collection type would be Enumerable and able to query the data store. 
        let dictCollectionTypes = Dictionary<ProvidedTypeDefinition, ProvidedTypeDefinition * ProvidedTypeDefinition>(HashIdentity.Reference)        // collection type * individual type

        /// Holds property container type such as birthPlaceContainer.  These types hold either the individuals for that property when in in the indivuals type system,
        /// Or the available classes with which to view the instances of that property (like a viewing lens) when in the general type system
        let propContainerTypes = Dictionary<string,ProvidedTypeDefinition>(HashIdentity.Structural)        // collection type * individual type

        //TODO: THESE TWO CORE RECUSRIVE FUNCTIONS THAT CREATE THE TYPE SYSTEM 
        //THE RESULT REALLY NEEDS TIDYING UP 

        let rec makePropertiesForRDFType (rdfTypeInGraph: string) (graphType:ProvidedTypeDefinition) isIndividual ultimateRootType =
            (if isIndividual then connector.getPropertiesOfRDFIndividual rdfTypeInGraph 
             else connector.getPropertiesOfRDFClassWithIndividualSamples(rdfTypeInGraph, schemaSampleAmount))
            |> List.filter (fst >> String.IsNullOrEmpty >> not)
            |> Seq.groupBy fst  // group by property name
            |> Seq.map(fun (propName,values) ->                 
                let typeName = rdfTypeInGraph + propName + "Container"                
                match propContainerTypes.TryGetValue typeName with
                | (true,propContainerType) -> ProvidedProperty(propName,propContainerType,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass) @@>) 
                | _ ->
                    // if this property group is the special "ns#type" the create the alternative provided type for it
                    // these properties let you view the current object as a different class 
                    if propName = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type" then
                        let altClassTypeName = rdfTypeInGraph + propName + "AlterativeTypes"

                        let altClassType = ProvidedTypeDefinition(altClassTypeName, Some typeof<RdfClass>, HideObjectMethods=true) 
                        altClassType.AddMember(ProvidedConstructor([ProvidedParameter("data",typeof<RdfClass>)],InvokeCode = fun args -> <@@ (%%args.[0]:RdfClass) @@> ))

                        values
                        |> Seq.distinctBy snd
                        |> Seq.iter( snd >> Option.iter( fun className -> 
                            // class name will be for example "Actor" OR "Person".  The return type the property will be the provided type for that class,
                            // and the instance that it returns is simply the same data that already have.  
                            let (altType,_) = findOrCreateType className ultimateRootType false false ultimateRootType  
                            altClassType.AddMember(ProvidedProperty(className,altType,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass) @@>))))

                        graphType.AddMember(altClassType) 
                        let prop = ProvidedProperty("As Class",altClassType, GetterCode = fun args -> <@@ (%%args.[0]:RdfClass) @@>)
                        prop.AddXmlDoc("View this object as another of its supported classes, as determined by the ns#type properties")
                        prop
                    else
                        // create a new provided type if it doesn't already exist
                        let propContainerType = ProvidedTypeDefinition(typeName, Some typeof<RdfClass>, HideObjectMethods=true)
                        propContainerType.AddMember(ProvidedConstructor([ProvidedParameter("data",typeof<RdfClass>)],InvokeCode = fun args -> <@@ (%%args.[0]:RdfClass) @@> ))
                        values
                        |> Seq.distinctBy snd
                        |> Seq.iter( fun (key,value) -> 
                            match value with
                            | None -> // this is a literal 
                                      propContainerType.AddMemberDelayed(fun _ -> ProvidedProperty("Literal",typeof<string list>,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass).GetLiteralValue(key) @@> ))
                            | Some(classOrResourceName) ->                            
                                let (providedPropertyType,alreadyExists) =
                                     findOrCreateType classOrResourceName ultimateRootType false true ultimateRootType
                                let listType = typedefof<list<_>>.MakeGenericType([|providedPropertyType :> Type|])
                                if isIndividual then                                    
                                    propContainerType.AddMemberDelayed(fun _ -> ProvidedProperty(classOrResourceName,providedPropertyType  ,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass).GetComplexValue((key,classOrResourceName)) @@>))
                                else
                                    propContainerType.AddMemberDelayed(fun _ -> ProvidedProperty(classOrResourceName,listType,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass).GetComplexValues((key)) @@>)))
                                        
                        graphType.AddMember propContainerType
                        propContainerTypes.Add(typeName,propContainerType)
                        ProvidedProperty(propName,propContainerType,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass) @@>))
            |> Seq.toList

        and findOrCreateType name containerType isRoot isIndividual ultimateRootType  =
            match dictClassTypes.TryGetValue name with 
            | false,_ -> 
                let t =                     
                    // individuals that are coming directly from a "Collection" type provided by the root, e.g., ActorCollection, all erase down to the Actor erased type
                    if isIndividual && containerType <> ultimateRootType then ProvidedTypeDefinition(name, baseType=Some(containerType:>_) ,HideObjectMethods=true)
                    // otherwise,  if this is an indivdual coming from another individual, then we don't know what type it is as it could be one of many, 
                    // in this case we are still returning the individual type but we are unable to erase it down to some other erased type (like actor)
                    // so just erase down to RdfClass,as is the case with all other types that are not individuals.
                    else ProvidedTypeDefinition(name, baseType=Some typeof<RdfClass>,HideObjectMethods=true)
                t.AddMemberDelayed( fun () -> ProvidedConstructor([],InvokeCode = fun _ -> <@@ RdfClass() @@>   ))
                t.AddMembersDelayed (fun () -> makePropertiesForRDFType name containerType isIndividual ultimateRootType)
                // the "isRoot" tells us if we are generating types for the "root" of the hierarchy, e.g., dbpedia.  This is the type which we create 
                // collection and individuals classes for all available types.
                match isRoot with
                | true ->             
                    // create collection and individuals type for this class
                    let individuals = ProvidedTypeDefinition(name + "Individuals",  Some typeof<obj>, HideObjectMethods=true)
                    let collection = ProvidedTypeDefinition(name + "Collection",  Some typeof<obj>, HideObjectMethods=true)
                
                    individuals.AddMembersDelayed(fun () -> 
                        // todo: use the runtime connector, it must be extracted from the underlying service type (which erases to connector) ? somehow ?
                        // or... maybe just make individuals erase down to connector and create a new instance of it ?
                        // just creating a new one at the moment/....
                                [ for ind in connector.getIndividuals(name,numOfIndividuals) |> Seq.distinctBy id  do   
                                    // here we find all the individuals of this type and create a proeprty for each one,
                                    // lazily (recursveily) populating the indiviual's unique type 
                                    let (individualType,_) = findOrCreateType ind t false true ultimateRootType
                                    let p = ProvidedProperty(ind, individualType, 
                                                            GetterCode = (fun args -> 
                                                            <@@
                                                                let connector = new Connector(schemaUrl)                                                            
                                                                RdfClass.Create(connector,ind)
                                                             @@>))
                                    yield p
                                ])

                    collection.AddMember( ProvidedProperty("Individuals", individuals, GetterCode = fun _ -> <@@ obj() @@>) ) 

                    t.AddMembers [individuals;collection]

                    serviceType.AddMember( ProvidedProperty(name + "Collection", collection, GetterCode = fun _ -> <@@ obj() @@> )) 
                    dictCollectionTypes.Add(t,(collection,individuals))
                | _ -> 
                    // otherwise this is not the root, so we just lazily populate the type with all of its members, be it an individual or not
                    containerType.AddMember t

                dictClassTypes.Add(name, t)
                
                (t,false)
            | _,t -> 
                (t,true)

        let insertRDFTypesForOneGraph (graph : ProvidedTypeDefinition, graphId) = 
            let allTypesForGraph = getRdfTypes  // graphId is not needed -- as only one graph
            let theNestedTypesForTheDataTypesClassForDomain = ResizeArray<_>()
            
            for rdfTypeInGraph in allTypesForGraph do
                let declaringType = graph
                let (itemType,_) = findOrCreateType rdfTypeInGraph graph true false graph
                declaringType.AddMember itemType
                
            theNestedTypesForTheDataTypesClassForDomain |> Seq.toArray

        do serviceType.AddMembers(            
            let makeTypeForGraph(graphName:string) = 
                let graph = ProvidedTypeDefinition(graphName, Some typeof<obj>,HideObjectMethods=false)
                graph.AddMembers(insertRDFTypesForOneGraph (graph,graphName) |> Array.toList) 
                graph
            [ for graph in connector.getGraphs() do 
                yield makeTypeForGraph (graph) ] )
          
        let rootType = ProvidedTypeDefinition(asm, ns, rootTypeName, baseType=Some typeof<Connector>, HideObjectMethods=true)
        rootType.AddMember serviceType
        rootType.AddMembers( 
            [ let meth =
                ProvidedMethod("GetDataContext", [], 
                               serviceType, IsStaticMethod = true,
                               InvokeCode = (fun _ -> <@@ Connector(schemaUrl) @@>))
              meth.AddXmlDoc "<summary>Returns an isntance of the RDF provider using the static paramters</summary>"
              yield meth ] )

        rootType
        
    let paramRdfType = ProvidedTypeDefinition(asm, ns, "RdfDataProvider", Some(typeof<obj>), HideObjectMethods = true)
    let schemaUrl = ProvidedStaticParameter("SchemaUrl",typeof<string>)    
    let individualsAmount = ProvidedStaticParameter("IndividualsAmount",typeof<int>,1000)
    let schemaSampleAmount = ProvidedStaticParameter("SchemaSampleAmount",typeof<int>,100)
    let helpText = "<summary>Some description of the RDF type provider</summary>                    
                   <param name='SchemaUrl'>Some description</param>                    
                   <param name='IndividualsAmount'>Some description</param>"                 
 
    do paramRdfType.DefineStaticParameters([schemaUrl;individualsAmount;schemaSampleAmount], fun typeName args -> 
        createTypes(args.[0] :?> string, // url
                    args.[1] :?> int,    // individuals amount
                    args.[2] :?> int,    // schema sample amount
                    typeName ))


    do paramRdfType.AddXmlDoc helpText                    
    do this.AddNamespace(ns, [paramRdfType])
    
[<TypeProviderAssembly>]
do ()












