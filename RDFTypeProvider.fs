namespace Samples.RdfTypeProvider

open System
open System.Reflection
open System.Collections.Generic
open System.IO
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open System.Text.RegularExpressions
    
// type provider implementation


[<TypeProvider>]
type RDFTypeProvider(config: TypeProviderConfig) as this =


    inherit TypeProviderForNamespaces()
    let ns = "Samples.RdfTypeProvider"
    let asm = Assembly.GetExecutingAssembly()


    let createTypes(schemaUrl, numOfIndividuals, rootTypeName) = 

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
        
        let dictClassTypes = Dictionary<string, ProvidedTypeDefinition>(HashIdentity.Structural)
        let dictObjectPropTypes = Dictionary<(string * string), ProvidedTypeDefinition>(HashIdentity.Structural)

        let dictCollectionTypes = Dictionary<ProvidedTypeDefinition, ProvidedTypeDefinition * ProvidedTypeDefinition>(HashIdentity.Reference)        // collection type * individual type

        let dictProperties = Dictionary<string, ProvidedProperty>(HashIdentity.Structural)

        // pluralize
        // containerTypeNameForDomainTypes  is simplified
        

        let rec makeMembersForRDFType (rdfTypeInGraph: string) (graphType:ProvidedTypeDefinition) =
            connector.getPropertiesOfRDFClass rdfTypeInGraph
            |> List.filter (fst >> String.IsNullOrEmpty >> not)
            |> List.map( fun (property,className) -> 
                    match className with
                    | Some(className) -> 
                        // retrieve or create the complex return type of the property, only return the type from here if it didn't already exist
                        // (otherwise the type will be added twice into the serviceType)
                        let (providedPropertyType,alreadyExists) = findOrCreateClassType className graphType
                        if alreadyExists then ProvidedProperty(property,providedPropertyType,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass).GetValue(property) @@>) 
                        else graphType.AddMember providedPropertyType
                             ProvidedProperty(property,providedPropertyType,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass).GetValue(property) @@>) 
                    | None -> ProvidedProperty(property,typeof<string>,GetterCode = fun args -> <@@ (%%args.[0]:RdfClass).GetValue(property) @@>) )
                  

        and findOrCreateClassType name graphType =
            match dictClassTypes.TryGetValue name with 
            | false,_ -> 
                let t = ProvidedTypeDefinition(name, baseType=Some typeof<RdfClass>,HideObjectMethods=true)
                t.AddMemberDelayed( fun () -> ProvidedConstructor([],InvokeCode = fun _ -> <@@ RdfClass() @@>   ))
                t.AddMembersDelayed (fun () -> makeMembersForRDFType name graphType)

                // create collection and individuals type for this class
                let individuals = ProvidedTypeDefinition(name + "Individuals",  Some typeof<obj>, HideObjectMethods=true)
                let collection = ProvidedTypeDefinition(name + "Collection",  Some typeof<obj>, HideObjectMethods=true)
                
                individuals.AddMembersDelayed(fun () -> 
                    // todo: use the runtime connector, it must be extracted from the underlying service type (which erases to connector) ? somehow ?
                    // or... maybe just make individuals erase down to connector and create a new instance of it ?
                    // just creating a new one at the moment/....
                            [ for ind in connector.getIndividuals(name,numOfIndividuals) |> Seq.distinctBy id  do                                 
                                let p = ProvidedProperty(ind, t, 
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

                dictClassTypes.Add(name, t)
                dictCollectionTypes.Add(t,(collection,individuals))
                (t,false)
            | _,t -> 
                (t,true)

  


        let insertRDFTypesForOneGraph (theDataTypesClassForGraph : ProvidedTypeDefinition, graphId) = 
            let allTypesForGraph = getRdfTypes  // graphId is not needed -- as only one graph
            let theNestedTypesForTheDataTypesClassForDomain = ResizeArray<_>()
            
            for rdfTypeInGraph in allTypesForGraph do
                let declaringType = theDataTypesClassForGraph
                let (itemType,_) = findOrCreateClassType rdfTypeInGraph theDataTypesClassForGraph
                declaringType.AddMember itemType
                
            theNestedTypesForTheDataTypesClassForDomain |> Seq.toArray




        do serviceType.AddMembers(            
                let makeTypeForGraphTypes(graphName:string) = 
                    let theDataTypesClassForGraph = ProvidedTypeDefinition(graphName, Some typeof<obj>,HideObjectMethods=false)
                    theDataTypesClassForGraph.AddMembers(insertRDFTypesForOneGraph (theDataTypesClassForGraph,graphName) |> Array.toList) 
                    theDataTypesClassForGraph


                [ for graph in connector.getGraphs() do 
                    yield makeTypeForGraphTypes (graph) ] )
              
          
        let rootType = ProvidedTypeDefinition(asm, ns, rootTypeName, baseType=Some typeof<obj>, HideObjectMethods=true)
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
    let helpText = "<summary>Some description of the RDF type provider</summary>                    
                   <param name='SchemaUrl'>Some description</param>                    
                   <param name='IndividualsAmount'>Some description</param>"                 
 
    do paramRdfType.DefineStaticParameters([schemaUrl;individualsAmount], fun typeName args -> 
        createTypes(args.[0] :?> string, // url
                    args.[1] :?> int,    // individuals amount
                    typeName ))


    do paramRdfType.AddXmlDoc helpText                    
    do this.AddNamespace(ns, [paramRdfType])
    
[<TypeProviderAssembly>]
do ()












