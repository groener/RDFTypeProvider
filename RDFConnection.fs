module RDFConnection 


open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open VDS.RDF.Query
open VDS.RDF.Parsing


// get types from domain and range of properties
// helper flatten the list
let rec concatList l =
    match l with
    | head :: tail -> head @ (concatList tail)
    | [] -> []
    




[<AutoOpen>]
type Connector(uri : string) =
    // result.Value does not work -- I don't know why
    // therefore thee following function to remove the variable, i.e.,
    // "?t = http://schema.org/LandmarksOrHistoricalBuildings" becomes "http://schema.org/LandmarksOrHistoricalBuildings"
    let endpoint = new VDS.RDF.Query.SparqlRemoteEndpoint(new Uri(uri))
    
    
    let getValueOfResult(binding : string) =
        let position = binding.IndexOf("=")
        let subbinding = binding.Substring(position + 1)
        subbinding.Trim()


        // currently, there is only one graph
    member this.getGraphs() =
        [uri]


//  get explicit types: use "rdf:type"
     // this function is tested and works
    member this.getExplicitTypes()  =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?t WHERE { ?_s rdf:type ?t } LIMIT 80")
        [for result in results ->  (getValueOfResult(result.ToString()))]
     
    // get all properties of a RDFClass (not instance) -- argument it the class name
    // e.g. SELECT DISTINCT ?property WHERE { <http://dbpedia.org/ontology/Person>  ?property  ?_x } LIMIT 100
          // this function is tested and works
    member this.getPropertiesOfRDFClass(className : string) =
        let query = String.Concat(["SELECT DISTINCT ?property WHERE { <" ; className ;"> ?property ?_x } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]
    
   
     // select only properties with resources in the range (no literals!)
    member this.getPropertiesOfRDFClassWithResourceRange(className : string) =
        let query = String.Concat(["SELECT DISTINCT ?property WHERE { <" ; className ;"> ?property ?_x . FILTER (!isLiteral(?_x)) } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]

    // select onyl properties with literals in the range
    member this.getPropertiesOfRDFClassWithLiteralRange(className : string) =
        let query = String.Concat(["SELECT DISTINCT ?property WHERE { <" ; className ;"> ?property ?_x . FILTER (isLiteral(?_x))  } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]
   
   
    // extend the previous function get the range classes -- argument is the class and the property
      // e.g. SELECT DISTINCT ?t WHERE { rdfs:label rdfs:range  ?t } LIMIT 100
      // tested and works 
    member this.getObjectOfRDFClassAndProperty(className : string, propertyName : string) =
        let query = String.Concat(["SELECT DISTINCT  ?object WHERE { <" ; className ;"> <"; propertyName ; "> ?object } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]
    


    // get alll elemetns that are defined as range for a given property
         // function is tested and works
    member this.getRangeTypes (rdfPropertyName : string) =
        let query = String.Concat(["SELECT DISTINCT ?t WHERE { <" ; rdfPropertyName ; "> rdfs:range  ?t } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query);
        [for result in results ->  (getValueOfResult(result.ToString()))]


    // e.g. let cls2 = analyzer.getRangeTypesOfClass("http://dbpedia.org/ontology/Person", "http://www.w3.org/2000/01/rdf-schema#subClassOf")
    //  tested
    member this.getRangeTypesOfClass (domainClass, propName) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; domainClass ; "> <" ; propName ; "> ?cls } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query);
        [for result in results -> (getValueOfResult(result.ToString()))]






    // get all elements that are defined as domain for a given property
       // function is tested and works
    member this.getDomainTypes (rdfPropertyName : string) =
        let query = String.Concat(["SELECT DISTINCT ?t WHERE { <" ; rdfPropertyName ; "> rdfs:domain  ?t } LIMIT 100 "])
        let results = endpoint.QueryWithResultSet(query);
        [for result in results -> (getValueOfResult(result.ToString()))]




    // get all explicit PROPERTIES 
        // tested and works
    member this.getExplicitProperties() =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?prop rdf:type rdf:Property} LIMIT 100 " )
        [for result in results -> (getValueOfResult(result.ToString()))]




    // get ALL Properties --- all  URIs that are used as predicates
     // --> retrieving all properties does not make sense !!!
    member this.getAllProperties() =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?_x ?prop ?_y  } LIMIT 100 ")
        [for result in results -> (getValueOfResult(result.ToString()))]




   // similar to getExplicitProperties :  get all explict ObjectProperties        (as object properties linkd to other URIs)
      // tested and works
    member this.getExplicitObjectProperties() =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?prop rdf:type owl:ObjectProperty} LIMIT 100 " )
        [for result in results -> (getValueOfResult(result.ToString()))]




   // get INDIVIDUALS of a class
   // tested, e.g., select distinct ?Concept where {?Concept rdf:type <http://dbpedia.org/ontology/PoliticalParty>} LIMIT 100
    member this.getIndividuals(className : string, ?limit : int ) =
        let limit = match limit with Some v -> v | _ -> 100
        let query = sprintf "SELECT DISTINCT ?ind WHERE { ?ind rdf:type  <%s> } LIMIT %i" className limit
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]
        


    member this.getClassOfIndividual(individual: string) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; individual ;"> rdf:type ?cls } LIMIT 100" ])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]




    member this.getSuperClass(className : string) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; className ; "> rdfs:subClassOf ?cls } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]






    member this.getPropertyTypesForUri(uri: string)  =        
        let queryString = String.Concat(["SELECT DISTINCT p? WHERE { <" ; uri ;"> rdf:type ?p } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(queryString)
        [for result in results -> (getValueOfResult(result.ToString()))]




 
  


