namespace Samples.RdfTypeProvider


open RDFConnection
open RDFDataStructure
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

        let dictProperties = Dictionary<string, ProvidedProperty>(HashIdentity.Structural)


        
        // pluralize
        // containerTypeNameForDomainTypes  is simplified
        

        let findOrCreateClassType name =
            match dictClassTypes.TryGetValue name with 
            | false,_ -> 
                let t = ProvidedTypeDefinition(name, baseType=Some typeof<obj>,HideObjectMethods=true)
                t.HideObjectMethods <- true
                t.AddMembersDelayed(fun () -> 
                [ for ind in connector.getRangeTypes name |> Seq.truncate numOfIndividuals |> Seq.distinctBy id  do 
                    let p = ProvidedProperty(ind, typeof<string>,
                                            GetterCode = (fun args -> <@@(%%(args.[0]):obj) :?> string  @@>))
                    yield p
                ])
                dictClassTypes.Add(name, t)
                t
            | _,t -> 
                t
//
        let findOrCreateObjectPropType(domainClass, propName) =
            let key = (domainClass, propName)
            match dictObjectPropTypes.TryGetValue key with 
            | false,_ -> 
                let t = ProvidedTypeDefinition(propName, baseType=Some typeof<obj>,HideObjectMethods=true)
                t.HideObjectMethods <- true
                t.AddMembersDelayed(fun () -> [ for rangeTy in connector.getRangeTypesOfClass (domainClass, propName)  do 
                                                        let p = findOrCreateClassType(rangeTy)
                                                        yield p ])
                dictObjectPropTypes.Add(key, t)
                t
            | _,t -> 
                t
          
        let makeMembersForRDFType (rdfTypeInGraph: string) =
            [ for propertyName in connector.getPropertiesOfRDFClass rdfTypeInGraph do
                    if not (String.IsNullOrEmpty propertyName) then 
                        // XXX not implemented yet: "staticPropertyType" and "runtimePropertyType"
                        // let staticPropertyType = property.FSharpPropertyType(fbSchema, refinedFSharpTypeOfFreebaseProperty, tryFindRefinedTypeForFreebaseType, makeDesignTimeNullableTy, makeDesignTimeSeqTy)
                        // let runtimePropertyType = property.FSharpPropertyRuntimeType(fbSchema, fbRuntimeInfo.FreebaseObjectType)
                        // now trying with type
                        //let p = findOrCreateProvidedProp(propertyName)
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
                        let t = ProvidedTypeDefinition(rdfTypeInGraph + "Individuals",  Some typeof<seq<RDFClassType>>, HideObjectMethods=true)
                        t.AddMembersDelayed(fun () -> 
                            [ for ind in connector.getIndividuals(rdfTypeInGraph,numOfIndividuals) |> Seq.distinctBy id  do 
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
                               InvokeCode = (fun _ ->
                                <@@ RDFConnection.Connector(schemaUrl) @@>))
              meth.AddXmlDoc "<summary>Returns an isntance of the RDF provider using the static paramters"
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






