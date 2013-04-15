namespace RDFTypeProvider

open RDFConnection.Connector
open RDFDataStructure
open System
open System.Reflection
open System.Collections.Generic
open System.IO
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open System.Text.RegularExpressions
open VDS.RDF.Query
open VDS.RDF.Parsing





    
[<TypeProvider>]
type RDFTypeProvider(config: TypeProviderConfig) as this =

    inherit TypeProviderForNamespaces()
    let ns = "MSR.RDFTypeProvider"
    let asm = Assembly.GetExecutingAssembly()

    let createTypes(sourceUri, rootTypeName, numOfIndividuals) = 
        
        let endpoint = new SparqlRemoteEndpoint(new Uri(sourceUri))
        
        let serviceTypesClass = ProvidedTypeDefinition("ServiceTypes",baseType=Some typeof<obj>,HideObjectMethods=false)
       
        let getRdfTypes =
            // FIRST : get explicit types
            let rdfTypesExplicit = getExplicitTypes endpoint
        
            // SECOND: now we extend types from domains
            let explicitRDFProperties = getExplicitProperties endpoint
            let explicitRDFObjectProperties = getExplicitObjectProperties endpoint

            let rdfTypesOfDomain =  List.concat [ for property in explicitRDFProperties ->  getDomainTypes(endpoint, property) ]

            // Combine types:  Explicit, Domain (and also Range types)
            rdfTypesExplicit  @ rdfTypesOfDomain |> Seq.distinctBy id
        
        let dictClassTypes = Dictionary<string, ProvidedTypeDefinition>(HashIdentity.Structural)
        let dictObjectPropTypes = Dictionary<(string * string), ProvidedTypeDefinition>(HashIdentity.Structural)

        let dictProperties = Dictionary<string, ProvidedProperty>(HashIdentity.Structural)


        
        // pluralize
        // containerTypeNameForDomainTypes  is simplified
        

        let findOrCreateClassType name =
                        match dictClassTypes.TryGetValue name with 
                        | false,_ -> 
                            let t = ProvidedTypeDefinition(name, baseType=Some typeof<obj>,HideObjectMethods=true)
                            t.HideObjectMethods <- true
                            t.AddMembersDelayed(fun () -> 
                            [ for ind in getRangeTypes(endpoint, name) |> Seq.truncate numOfIndividuals |> Seq.distinctBy id  do 
                                let p = ProvidedProperty(ind, typeof<string>,
                                                        GetterCode = (fun args -> <@@(%%(args.[0]):obj) :?> string  @@>))
                                yield p
                            ])
                            dictClassTypes.Add(name, t)
                            t
                        | _,t -> 
                            t

        let findOrCreateObjectPropType(domainClass, propName) =
                        let key = (domainClass, propName)
                        match dictObjectPropTypes.TryGetValue key with 
                        | false,_ -> 
                            let t = ProvidedTypeDefinition(propName, baseType=Some typeof<obj>,HideObjectMethods=true)
                            t.HideObjectMethods <- true
                            t.AddMembersDelayed(fun () -> [ for rangeTy in getRangeTypesOfClass (endpoint, domainClass, propName)  do 
                                                                    let p = findOrCreateClassType(rangeTy)
                                                                    yield p ])
                            dictObjectPropTypes.Add(key, t)
                            t
                        | _,t -> 
                            t


        
        let findOrCreateProvidedProp name =
                        let key = name
                        match dictProperties.TryGetValue key with 
                        | false,_ -> 
                            let p = ProvidedProperty(name, typeof<string>, IsStatic = true, GetterCode = fun args ->  <@@ (%%(args.[0]): obj) :?> string  @@> )
                            dictProperties.Add(key, p)
                            p
                        | _,p -> 
                            p
  
          
        let makeMembersForRDFType (rdfTypeInGraph: string) =
                    [ for propertyName in getPropertiesOfRDFClass(endpoint, rdfTypeInGraph) do
                            if not (String.IsNullOrEmpty propertyName) then 
                                let p = findOrCreateObjectPropType(rdfTypeInGraph, propertyName)
                                yield (p :> MemberInfo) 
                           ]
  

        let insertRDFTypesForOneGraph (theDataTypesClassForGraph : ProvidedTypeDefinition, graphId) = 
            let allTypesForGraph = getRdfTypes  // graphId is not needed -- as only one graph

            // Collect up the immediate nested types -- not yet done
            let theNestedTypesForTheDataTypesClassForDomain = ResizeArray<_>()
            
            
            
            for rdfTypeInGraph in allTypesForGraph do
                // XXX not sure whether the next lines are needed ???
                //let fullPath = pathToTypeForFreebaseType rdfTypeInGraph
                //let path, typeName = List.frontAndBack fullPath
                //let _domain, path = List.headAndTail path
                //let declaringType = (theDataTypesClassForDomain, path) ||> List.fold findOrCreateEnclosingType
                let declaringType = theDataTypesClassForGraph
                let itemType = 
                    let t = findOrCreateClassType(rdfTypeInGraph)
                 //   t.SetAttributes (TypeAttributes.Public ||| TypeAttributes.Interface ||| enum (int32 TypeProviderTypeAttributes.IsErased))
                    //t.AddInterfaceImplementationsDelayed(fun () -> [RDFClassType])
                    t.AddMembersDelayed (fun () -> makeMembersForRDFType rdfTypeInGraph)
                   // t.AddMembers (makeMembersForRDFType rdfTypeInGraph)
                 // TODO
                 //   t.AddInterfaceImplementationsDelayed(fun () -> 
                 //     [ for ity in rdfTypeInGraph.IncludedTypes do 
                 //         match tryFindRefinedTypeForFreebaseTypeId (ity.Domain, ity.Id) with 
                 //         | Some i -> yield i
                 //         | None -> 
                 //             //System.Diagnostics.Debug.Assert(false,"included type not found")
                 //             () ])

                    t
 
                     
                let individualsType = 
                    if numOfIndividuals > 0 then 
                        let t = ProvidedTypeDefinition(rdfTypeInGraph + "Individuals",  Some typeof<seq<string>>, HideObjectMethods=true)
                        t.AddMembersDelayed(fun () -> 
                            [ for ind in getIndividuals(endpoint, rdfTypeInGraph) |> Seq.truncate numOfIndividuals |> Seq.distinctBy id  do 
                                let p = ProvidedProperty(ind, typeof<string>, IsStatic = true,
                                                        GetterCode = (fun args -> <@@(%%(args.[0]):obj) :?> string  @@>))
                                yield p
                            ])
                        Some t
                    else 
                        None


                Option.iter itemType.AddMember individualsType   
                declaringType.AddMember itemType
                
            theNestedTypesForTheDataTypesClassForDomain |> Seq.toArray


        do serviceTypesClass.AddMember(
            let graphType = ProvidedTypeDefinition(sourceUri, Some typeof<obj>,HideObjectMethods=false)
            graphType.AddMembersDelayed(fun () -> insertRDFTypesForOneGraph (graphType, sourceUri) |> Array.toList) 
                //theDataTypesClassForGraph.AddMembers(insertRDFTypesForOneGraph (theDataTypesClassForGraph, graphName) |> Array.toList)
            graphType)
            
        let theRootType = ProvidedTypeDefinition(asm, ns, "RDFData", baseType=Some typeof<obj>, HideObjectMethods=false)
        theRootType.AddMember  serviceTypesClass 
        //theRootType.AddMembersDelayed (fun () -> 
        //    [ yield ProvidedMethod ("GetRDFData", [], serviceType, IsStaticMethod=true,
          //                          InvokeCode = (fun args -> <@@ " test " @@> ))
                                    // XXX - 1   I cannot resolve the following line
                                    //InvokeCode = (fun _args -> Expr.Call(createDataContext, [  Expr.Value apiKey; Expr.Value proxyPrefix; Expr.Value serviceUrl; Expr.Value useUnits; Expr.Value snapshotDate; Expr.Value useLocalCache; Expr.Value allowQueryEvaluateOnClientSide  ])))
         //   ])
        theRootType

        
    
    let rdfRootType =  createTypes("http://dbpedia.org/sparql", ns, 50)
          
    do this.AddNamespace(ns, [rdfRootType])
    
[<TypeProviderAssembly>]
do ()






