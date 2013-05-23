namespace Samples.RdfTypeProvider

open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open VDS.RDF.Query
open VDS.RDF.Parsing

[<AutoOpen>]
type Connector(uri : string) =
    // result.Value does not work -- I don't know why
    // therefore thee following function to remove the variable, i.e.,
    // "?t = http://schema.org/LandmarksOrHistoricalBuildings" becomes "http://schema.org/LandmarksOrHistoricalBuildings"
    let endpoint = new VDS.RDF.Query.SparqlRemoteEndpoint(new Uri(uri))   

    let queryExecuting = Event<_>()

    member this.QueryExecuting = queryExecuting.Publish

    // currently, there is only one graph
    member this.getGraphs() =
        [uri]


//  get explicit types: use "rdf:type"
    member this.getExplicitTypes()  =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?t WHERE { ?_s rdf:type ?t } LIMIT 80 ")
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList
     
   
     // select only properties with resources in the range (no literals!)
    member this.getPropertiesOfRDFClassWithResourceRange(className : string) =
        try
            let query = String.Concat(["SELECT DISTINCT ?property ?c ?object WHERE { <" ; className ;"> ?property ?object . FILTER (!isLiteral(?object)). ?object rdf:type ?c }"])        
            let results = endpoint.QueryWithResultSet(query)
            results.Results |> Seq.map(fun result -> (result.[0].ToString(),Some(result.[1].ToString()))) |> Seq.toList
        with 
        | _ -> []
        
    // select onyl properties with literals in the range
    member this.getPropertiesOfRDFClassWithLiteralRange(className : string) =
        try
            let query = String.Concat(["SELECT DISTINCT ?property ?c WHERE { <" ; className ;"> ?property ?c . FILTER (isLiteral(?c))  } "])
            let results = endpoint.QueryWithResultSet(query)
            results.Results |> Seq.map(fun result -> (result.[0].ToString(),None)) |> Seq.toList
        with 
        | _ -> []
   
    // get all properties of a RDFClass (not instance) -- argument it the class name
    // e.g. SELECT DISTINCT ?property WHERE { <http://dbpedia.org/ontology/Person>  ?property  ?_x } LIMIT 100
          // this function is tested and works
    member this.getPropertiesOfRDFClass(className : string) =
        this.getPropertiesOfRDFClassWithResourceRange className @ this.getPropertiesOfRDFClassWithLiteralRange className
        
    member this.getPropertiesOfRDFClassWithIndividualSamples(className : string, sampleAmount : int) =
        // for each individual get the resrouce and literal properties and form a distinct list of them        

        //TODO: for "thing" ``http://www.w3.org/2002/07/owl#Thing`` we don't want to sample from individuals, jus get the 
        // explicty defined resoruces and literals
        let mapper className = this.getPropertiesOfRDFClassWithResourceRange className @ this.getPropertiesOfRDFClassWithLiteralRange className
        this.getIndividuals(className, sampleAmount)
        |> Seq.map mapper
        |> Seq.collect id
        // group the properties by property name
        |> Seq.groupBy fst
        //for each property group, we want to pick only those that appear most        
        // perform a count based on the value of the property
        |> Seq.map(fun (key,values) ->    
            let countedValues = 
                values
                |> Seq.countBy snd
                |> Seq.sortBy snd
                |> Seq.toList
                |> List.rev 
            
            //TODO: Some clever stuff here to get the optimal top values 
            // eg if there are 200+ results but only the top 20 have the majority of the count,
            // we don't want to include lots of very low counts as well 

            // taking the top 25% for now
            let amountToTake = countedValues.Length / 4 
            let amountToTake  = if amountToTake < 1 then  1 else amountToTake
            let amountToTake  = if countedValues.Length <= 5 then countedValues.Length else amountToTake

            countedValues 
            |> Seq.take amountToTake
            |> Seq.map( fun (value,_) -> (key,value))
            |> Seq.toList)
        |> Seq.collect id
        |> Seq.toList    
   

   
    // extend the previous function get the range classes -- argument is the class and the property
      // e.g. SELECT DISTINCT ?t WHERE { rdfs:label rdfs:range  ?t } LIMIT 100
      // tested and works 
    member this.getObjectOfRDFClassAndProperty(className : string, propertyName : string) =
        let query = String.Concat(["SELECT DISTINCT  ?object WHERE { <" ; className ;"> <"; propertyName ; "> ?object } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList
    


    // get alll elemetns that are defined as range for a given property
         // function is tested and works
    member this.getRangeTypes (rdfPropertyName : string) =
        let query = String.Concat(["SELECT DISTINCT ?t WHERE { <" ; rdfPropertyName ; "> rdfs:range  ?t } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query);
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList


    // e.g. let cls2 = analyzer.getRangeTypesOfClass("http://dbpedia.org/ontology/Person", "http://www.w3.org/2000/01/rdf-schema#subClassOf")
    //  tested
    member this.getRangeTypesOfClass (domainClass, propName) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; domainClass ; "> <" ; propName ; "> ?cls } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query);
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList






    // get all elements that are defined as domain for a given property
       // function is tested and works
    member this.getDomainTypes (rdfPropertyName : string) =
        let query = String.Concat(["SELECT DISTINCT ?t WHERE { <" ; rdfPropertyName ; "> rdfs:domain  ?t } LIMIT 100 "])
        let results = endpoint.QueryWithResultSet(query);
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList




    // get all explicit PROPERTIES 
        // tested and works
    member this.getExplicitProperties() =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?prop rdf:type rdf:Property} LIMIT 100 " )
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList




    // get ALL Properties --- all  URIs that are used as predicates
     // --> retrieving all properties does not make sense !!!
    member this.getAllProperties() =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?_x ?prop ?_y  } LIMIT 100 ")
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList




   // similar to getExplicitProperties :  get all explict ObjectProperties        (as object properties linkd to other URIs)
      // tested and works
    member this.getExplicitObjectProperties() =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?prop rdf:type owl:ObjectProperty} LIMIT 100 " )
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList




   // get INDIVIDUALS of a class
   // tested, e.g., select distinct ?Concept where {?Concept rdf:type <http://dbpedia.org/ontology/PoliticalParty>} LIMIT 100
    member this.getIndividuals(className : string, ?limit : int ) =
        let limit = match limit with Some v -> v | _ -> 100
        let query = sprintf "SELECT DISTINCT ?ind WHERE { ?ind rdf:type  <%s> } LIMIT %i" className limit
        let results = endpoint.QueryWithResultSet(query)
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList
        

    // Working with individuals
    // for an individual get all its classes 
//    member this.getClassesOfIndividual(individual: string) =
//        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; individual ;"> rdf:type ?cls } LIMIT 100" ])
//        let results = endpoint.QueryWithResultSet(query)
//        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList

        
    // get properties where the range is an individual of a resource type 
      // this is similar to getPropertiesOfRDFClassWithResourceRange ... but here for individuals instead of classes
    member this.getPropertiesOfIndWithResourceRange(individual: string) =
        let query = String.Concat(["SELECT DISTINCT ?property ?resource ?c WHERE { <" ; individual ;">  ?property ?resource . FILTER (!isLiteral(?resource)) FILTER (!isBlank(?resource)). ?resource rdf:type ?c } " ])
        queryExecuting.Trigger(sprintf "getting resource properties of individual : %s " query)
        let results = endpoint.QueryWithResultSet(query)
        results.Results |> Seq.map(fun result -> (result.[0].ToString(),result.[1].ToString(),result.[2].ToString())) |> Seq.toList

      
    // get properties where the range is a literal 
      // this is similar to getPropertiesOfRDFClassWithResourceRange ... but here for individuals instead of classes
    member this.getPropertiesOfIndWithLiteralRange(individual: string) =
        let query = String.Concat(["SELECT DISTINCT ?property ?resource WHERE { <" ; individual ;">  ?property ?resource . FILTER (isLiteral(?resource)) } " ])
        queryExecuting.Trigger(sprintf "getting literal properties of individual : %s " query)
        let results = endpoint.QueryWithResultSet(query)
        results.Results |> Seq.map(fun result -> (result.[0].ToString(),result.[1].ToString())) |> Seq.toList


    member this.getPropertiesOfRDFIndividual(uri : string) =
        (this.getPropertiesOfIndWithResourceRange uri |> List.map(fun (a,b,_) -> (a,Some b))) 
        @ 
        (this.getPropertiesOfIndWithLiteralRange uri|> List.map(fun (a,_) -> (a,None)))
        

    // not used at the moment

    member this.getSuperClass(className : string) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; className ; "> rdfs:subClassOf ?cls } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList


    member this.getPropertyTypesForUri(uri: string)  =        
        let queryString = String.Concat(["SELECT DISTINCT p? WHERE { <" ; uri ;"> rdf:type ?p } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(queryString)
        results.Results |> Seq.map(fun result -> (result.[0].ToString())) |> Seq.toList




 
  


