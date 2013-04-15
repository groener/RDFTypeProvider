namespace RDFConnection 

open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open VDS.RDF.Query
open VDS.RDF.Parsing
open RDFDataStructure 


module Connector =
    
    let private getValueOfResult(binding : string) =
        let position = binding.IndexOf("=")
        let subbinding = binding.Substring(position + 1)
        subbinding.Trim()
        
     //  get explicit types: use "rdf:type"
     // this function is tested and works
    let getExplicitTypes (endpoint:SparqlRemoteEndpoint) =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?t WHERE { ?_s rdf:type ?t } LIMIT 80")
        [for result in results ->  (getValueOfResult(result.ToString()))]
     
  
    // get all properties of a RDFClass (not instance) -- argument it the class name
    // e.g. SELECT DISTINCT ?property WHERE { <http://dbpedia.org/ontology/Person>  ?property  ?_x } LIMIT 100
          // this function is tested and works
    let getPropertiesOfRDFClass(endpoint: SparqlRemoteEndpoint, className : string) =
        let query = String.Concat(["SELECT DISTINCT ?property WHERE { <" ; className ;"> ?property ?_x } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]
    
    // extend the previous function get the range classes -- argument is the class and the property
      // e.g. SELECT DISTINCT ?t WHERE { rdfs:label rdfs:range  ?t } LIMIT 100
      // tested and works 
    let getObjectOfRDFClassAndProperty(endpoint: SparqlRemoteEndpoint, className : string, propertyName : string) =
        let query = String.Concat(["SELECT DISTINCT  ?object WHERE { <" ; className ;"> <"; propertyName ; "> ?object } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]
    

    // get alll elemetns that are defined as range for a given property
         // function is tested and works
    let getRangeTypes (endpoint: SparqlRemoteEndpoint, rdfPropertyName : string) =
        let query = String.Concat(["SELECT DISTINCT ?t WHERE { <" ; rdfPropertyName ; "> rdfs:range  ?t } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query);
        [for result in results ->  (getValueOfResult(result.ToString()))]

    // e.g. let cls2 = analyzer.getRangeTypesOfClass("http://dbpedia.org/ontology/Person", "http://www.w3.org/2000/01/rdf-schema#subClassOf")
    //  tested
    let getRangeTypesOfClass (endpoint: SparqlRemoteEndpoint, domainClass, propName) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; domainClass ; "> <" ; propName ; "> ?cls } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query);
        [for result in results -> (getValueOfResult(result.ToString()))]



    // get all elements that are defined as domain for a given property
       // function is tested and works
    let getDomainTypes (endpoint: SparqlRemoteEndpoint, rdfPropertyName : string) =
        let query = String.Concat(["SELECT DISTINCT ?t WHERE { <" ; rdfPropertyName ; "> rdfs:domain  ?t } LIMIT 100 "])
        let results = endpoint.QueryWithResultSet(query);
        [for result in results -> (getValueOfResult(result.ToString()))]


    // get all explicit PROPERTIES 
        // tested and works
    let getExplicitProperties(endpoint: SparqlRemoteEndpoint) =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?prop rdf:type rdf:Property} LIMIT 100 " )
        [for result in results -> (getValueOfResult(result.ToString()))]


    // get ALL Properties --- all  URIs that are used as predicates
     // --> retrieving all properties does not make sense !!!
    let getAllProperties(endpoint: SparqlRemoteEndpoint) =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?_x ?prop ?_y  } LIMIT 100 ")
        [for result in results -> (getValueOfResult(result.ToString()))]


   // similar to getExplicitProperties :  get all explict ObjectProperties        (as object properties linkd to other URIs)
      // tested and works
    let getExplicitObjectProperties(endpoint: SparqlRemoteEndpoint) =
        let results = endpoint.QueryWithResultSet("SELECT DISTINCT ?prop WHERE {?prop rdf:type owl:ObjectProperty} LIMIT 100 " )
        [for result in results -> (getValueOfResult(result.ToString()))]


   // get INDIVIDUALS of a class
   // tested, e.g., select distinct ?Concept where {?Concept rdf:type <http://dbpedia.org/ontology/PoliticalParty>} LIMIT 100
    let getIndividuals(endpoint: SparqlRemoteEndpoint, className : string) =
        let query = String.Concat(["SELECT DISTINCT ?ind WHERE { ?ind rdf:type  <" ; className ; "> } LIMIT 100" ])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]
        

    let getClassOfIndividual(endpoint: SparqlRemoteEndpoint, individual: string) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; individual ;"> rdf:type ?cls } LIMIT 100" ])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]


    let getSuperClass(endpoint: SparqlRemoteEndpoint, className : string) =
        let query = String.Concat(["SELECT DISTINCT ?cls WHERE { <" ; className ; "> rdfs:subClassOf ?cls } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(query)
        [for result in results -> (getValueOfResult(result.ToString()))]



    let getPropertyTypesForUri(endpoint: SparqlRemoteEndpoint, uri: string)  =        
        let queryString = String.Concat(["SELECT DISTINCT p? WHERE { <" ; uri ;"> rdf:type ?p } LIMIT 100"])
        let results = endpoint.QueryWithResultSet(queryString)
        [for result in results -> (getValueOfResult(result.ToString()))]


 
  


