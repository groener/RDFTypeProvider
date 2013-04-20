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
                // this needs to change - erase down to dictionary type which will containt instances of the property values at runtime
                // eg Actor 
                let t = ProvidedTypeDefinition(name, baseType=Some typeof<RdfClass>,HideObjectMethods=true)
                t.AddMemberDelayed( fun () -> ProvidedConstructor([],InvokeCode = fun _ -> <@@ RdfClass() @@>   ))
                t.AddMembersDelayed (fun () -> makeMembersForRDFType name graphType)

                // create collection and individuals type for this class
                let individuals = ProvidedTypeDefinition(name + "Individuals",  Some typeof<obj>, HideObjectMethods=true)
                let collection = ProvidedTypeDefinition(name + "Collection",  Some typeof<obj>, HideObjectMethods=true)
                
                individuals.AddMembersDelayed(fun () -> 
                    // todo: to use the runtime connector, it must be extracted from the underlying service type (which erases to connector) ? somehow ?
                    // orr... maybe just make individuals erase down to connector and create a new instance of it ?
                            [ for ind in connector.getIndividuals(name,numOfIndividuals) |> Seq.distinctBy id  do 
                                let p = ProvidedProperty(ind, typeof<string>, IsStatic = true,
                                                        GetterCode = (fun args -> <@@(%%(args.[0]):obj) :?> string  @@>))
                                yield p
                            ])

                collection.AddMember( ProvidedProperty("Individuals", individuals) ) 

                t.AddMembers [individuals;collection]

                serviceType.AddMember( ProvidedProperty(name + "Collection", collection) ) 

                dictClassTypes.Add(name, t)
                dictCollectionTypes.Add(t,(collection,individuals))
                (t,false)
            | _,t -> 
                (t,true)

  


        let insertRDFTypesForOneGraph (theDataTypesClassForGraph : ProvidedTypeDefinition, graphId) = 
            let allTypesForGraph = getRdfTypes  // graphId is not needed -- as only one graph


            // Collect up the immediate nested types -- not yet done
            let theNestedTypesForTheDataTypesClassForDomain = ResizeArray<_>()
            
            
            
            for rdfTypeInGraph in allTypesForGraph do
                let declaringType = theDataTypesClassForGraph
                let itemType = 
                    let (t,_) = findOrCreateClassType rdfTypeInGraph theDataTypesClassForGraph

                    t
                                      
                let individualsType = 
                    if numOfIndividuals > 0 then 
                        let t = ProvidedTypeDefinition(rdfTypeInGraph + "Individuals",  Some typeof<obj>, HideObjectMethods=true)
                        //t.AddMemberDelayed( fun () -> ProvidedConstructor([ProvidedParameter("data",typeof<string list>)], InvokeCode = fun args -> <@@ (%%args.[0]:(string list)) @@> ))
                        t.AddMembersDelayed(fun () -> 
                            [ for ind in connector.getIndividuals(rdfTypeInGraph,numOfIndividuals) |> Seq.distinctBy id  do 
                                let p = ProvidedProperty(ind, typeof<string>, IsStatic = true,
                                                        GetterCode = (fun args -> <@@(%%(args.[0]):obj) :?> string  @@>))
                                yield p
                            ])
                        Some t
                    else 
                        None

                     // two new functions in RDFConnection:
                      //  1. getPropertiesOfIndWithResourceRange(individual: string) --> to get properties of an individual (where the range of the property is a resource (uri)
                      //  2. getPropertiesOfIndWithLiteralRange(individual: string)  --> to get properties of an individual (where the range is a literal)


               //dataContext.Actors.Individuals 


                Option.iter itemType.AddMember individualsType   
                declaringType.AddMember itemType
                
            theNestedTypesForTheDataTypesClassForDomain |> Seq.toArray




        do serviceType.AddMembers(            
                let makeTypeForGraphTypes(graphName:string) = 
                    let theDataTypesClassForGraph = ProvidedTypeDefinition(graphName, Some typeof<obj>,HideObjectMethods=false)
                    theDataTypesClassForGraph.AddMembersDelayed(fun () -> insertRDFTypesForOneGraph (theDataTypesClassForGraph,graphName) |> Array.toList) 
                    //theDataTypesClassForGraph.AddMembers(insertRDFTypesForOneGraph (theDataTypesClassForGraph, graphName) |> Array.toList)
                    theDataTypesClassForGraph


                [ for graph in connector.getGraphs() do 
                    yield makeTypeForGraphTypes (graph) ] )
              
          
        let rootType = ProvidedTypeDefinition(asm, ns, rootTypeName, baseType=Some typeof<obj>, HideObjectMethods=true)
        rootType.AddMember serviceType
        rootType.AddMembersDelayed( fun () -> 
            [ let meth =
                ProvidedMethod("GetDataContext", [], 
                               serviceType, IsStaticMethod = true,
                               InvokeCode = (fun _ -> <@@ Connector(schemaUrl) @@>))
              meth.AddXmlDoc "<summary>Returns an isntance of the RDF provider using the static paramters</summary>"
              yield meth ] )




        //theRootType.AddMembersDelayed (fun () -> 
        //    [ yield ProvidedMethod ("GetRDFData", [], serviceType, IsStaticMethod=true,
          //                          InvokeCode = (fun args -> <@@ " test " @@> ))
                                    // XXX - 1   I cannot resolve the following line
                                    //InvokeCode = (fun _args -> Expr.Call(createDataContext, [  Expr.Value apiKey; Expr.Value proxyPrefix; Expr.Value serviceUrl; Expr.Value useUnits; Expr.Value snapshotDate; Expr.Value useLocalCache; Expr.Value allowQueryEvaluateOnClientSide  ])))
         //   ])
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












