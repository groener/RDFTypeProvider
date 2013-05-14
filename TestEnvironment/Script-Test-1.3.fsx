#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\RDFTypeProvider-git\RDFTypeProvider-git\RDFTypeProvider\bin\Debug\RDFTypeProvider_git.dll"



//type data = Samples.RdfTypeProvider.RdfDataProvider<"http://dbpedia.org/sparql", 50>

//let dc = Samples.RdfTypeProvider.RdfDataProvider<"http://dbpedia.org/sparql", 50>.GetDataContext()
let dc = Samples.RdfTypeProvider.RdfDataProvider<"http://biocyc.bio2rdf.org/sparql", 50>.GetDataContext()



let city = dc.



//let ac = Samples.RDFTypeProvider.
//type rdf = Samples.RdfTypeProvider.RdfDataProvider< "http://dbpedia.org/sparql", 100>
//let dc = rdf.GetDataContext()
//
//

//type actor = dc.getExplicitTypes.



let a = dc.``http://dbpedia.org/ontology/ActorCollection``.Individuals.``http://dbpedia.org/resource/Bruce_Lee``

a.``http://dbpedia.org/ontology/deathPlace``.``http://dbpedia.org/ontology/PopulatedPlace``.
            // this is not intended

//a.``http://dbpedia.org/ontology/Person/weight``.

//
//a.``http://dbpedia.org/ontology/abstract``.
//let l = ResizeArray< rdf.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor`` >()
//
//l.Add a
//
//list
//
//
//dc.``http://dbpedia.org/ontology/ProgrammingLanguageCollection``
//
//dc.``http://dbpedia.org/ontology/AgentCollection``.Individuals.``http://dbpedia.org/resource/Arpachshad``
//
//
//dc.``http://dbpedia.org/ontology/BaseballPlayerCollection``.Individuals.``http://dbpedia.org/resource/Bob_Lemon``.``http://www.w3.org/2000/01/rdf-schema#isDefinedBy``.``http://www.w3.org/2002/07/owl#versionInfo``
//
//
//
//type actor1 = data.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor``
//
//ac.``http://www.w3.org/2000/01/rdf-schema#subClassOf``.``http://www.w3.org/2000/01/rdf-schema#subClassOf``.``http://www.w3.org/2000/01/rdf-schema#subClassOf``.
//
//ac
//
//
//type artist = data.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Artist``
//
//let subArtist = artist.
//
//let ac = actor1()
//
//ac.GetValue.
//
//ac.``http://www.w3.org/2000/01/rdf-schema#subClassOf``.``http://www.w3.org/2000/01/rdf-schema#subClassOf``
//
//
//
//
//type artist = data.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor``
//
//type artist2 = data.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Artist``
//
//let ind = artist2.``http://dbpedia.org/ontology/ArtistIndividuals``.
//
//
//type person = data.RdfService.``http://dbpedia.org/sparql``.``Künstler@de``.Create
//
//type artist2 = 
//
//let ind = actor1.``http://dbpedia.org/ontology/ActorIndividuals``
//
//
//type IndActors = data.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor``.``http://dbpedia.org/ontology/ActorIndividuals``
//
//let firstActor = IndActors.``http://dbpedia.org/resource/Amber_Lynn``
//
//
//let ac1 = actor1()
//
//let acc = act1.
//
//
//ac1.SetValue("http://www.w3.org/2000/01/rdf-schema#subClassOf","xxx")
//
//
//
//ac1.``http://www.w3.org/2000/01/rdf-schema#subClassOf``
//
//
//let actor1 = data.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor``.``http://dbpedia.org/ontology/ActorIndividuals``.``http://dbpedia.org/resource/Anna_Malle``
//
//
//
//type act1 = MSR.RDFTypeProvider.RDFData.ServiceTypes.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor``.``http://www.w3.org/2000/01/rdf-schema#subClassOf``.``http://dbpedia.org/ontology/Artist``
//
//
//
//type artist = MSR.RDFTypeProvider.RDFData.ServiceTypes.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/BaseballPlayer``.
//
//
//
//type actor = MSR.RDFTypeProvider.RDFData.ServiceTypes.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor``.``http://dbpedia.org/ontology/ActorIndividuals``.``http://dbpedia.org/resource/Anita_Mui``.Chars
//
//
//type artist1 = MSR.RDFTypeProvider.RDFData.ServiceTypes.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor``.``http://www.w3.org/2000/01/rdf-schema#subClassOf``.``http://dbpedia.org/ontology/Artist``
//
//type artist2 = MSR.RDFTypeProvider.RDFData.ServiceTypes.``http://dbpedia.org/sparql``.``http://xmlns.com/foaf/0.1/Person``.``http://www.w3.org/2000/01/rdf-schema#subClassOf``.``http://xmlns.com/foaf/0.1/Agent``
//
//
//
//
//
//
////let service = MSR.RDFTypeProvider.
//
////let prop = service.
//
////let cc = rdf.
//
