#if INTERACTIVE
#r @"C:\Users\a-romcki\Documents\visual studio 2012\Projects\RdfProvider\RdfProvider\bin\Debug\RdfProvider.dll"
#endif


open Samples.RdfTypeProvider
type rdf = Samples.RdfTypeProvider.RdfDataProvider< "http://dbpedia.org/sparql/", 1000>
let dc = rdf.GetDataContext()








let a = dc.``http://dbpedia.org/ontology/ActorCollection``.Individuals.``http://dbpedia.org/resource/Bruce_Lee``


//a.``http://dbpedia.org/property/birthPlace``.``http://dbpedia.org/resource/San_Francisco_Chinese_Hospital``.``http://purl.org/dc/terms/subject``


//let dc.``http://dbpedia.org/ontology/ProgrammingLanguageCollection``.

let b = a.``http://dbpedia.org/ontology/award``.``http://dbpedia.org/resource/Fist_of_Fury`` //.``http://dbpedia.org/property/caption``.Literal
 
a.``http://dbpedia.org/property/birthPlace``.``http://dbpedia.org/resource/San_Francisco_Chinese_Hospital``

let asActor = a.``As Class``.``http://dbpedia.org/ontology/Person``

a.``http://dbpedia.org/ontology/birthPlace``.``http://dbpedia.org/resource/San_Francisco``.``http://dbpedia.org/ontology/leaderName``.``http://dbpedia.org/resource/Tom_Ammiano``.``http://dbpedia.org/property/religion``.``http://dbpedia.org/resource/Catholic_Church``.``http://dbpedia.org/ontology/abstract``.Literal

asActor.``http://dbpedia.org/ontology/birthPlace``.``http://dbpedia.org/ontology/Place``.[0].``http://dbpedia.org/ontology/leaderName``.``http://dbpedia.org/ontology/Person``.[1].``http://dbpedia.org/ontology/abstract``.Literal

let cou = rdf.RdfService.``http://dbpedia.org/sparql/``.``http://schema.org/Country``()

let people = new System.Collections.Generic.List<rdf.RdfService.``http://dbpedia.org/sparql/``.``http://dbpedia.org/ontology/Person``>()


people.Add( a.``As Class``.``http://dbpedia.org/ontology/Person``)

people



asActor.ForceLazyValues()

b.``http://dbpedia.org/ontology/abstract``.Literal

