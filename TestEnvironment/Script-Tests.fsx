#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\RDFtypeProver-svn1\RDF-TP-gg\MergedWB-based\RDF-TP2803\RDFTypeProvider\RDFTypeProvider\bin\Debug\RDFTypeProvider.dll"

//type RDF = MSR.RDFTypeProvider.MainRDFType<"http://www.semantic-systems-biology.org/biogateway/endpoint">
//type mainrdf = MSR.RDFTypeProvider.MainRDFType<"http://biocyc.bio2rdf.org/sparql">

let rdf = MSR.RDFTypeProvider.rootType.GetRDFData()




type RDF = MSR.RDFTypeProvider.MainRDFType<"http://dbpedia.org/sparql">
//type RDF = MSR.RDFTypeProvider.MainRDFType<"http://dbtune.org/musicbrainz/sparql">()


//type RDF = MSR.RDFTypeProvider.MainRDFType<"http://data.linkedmdb.org/sparql">()

let aClass = new RDF.RDFClass.``http://dbpedia.org/ontology/Actor``

let prop = aClass.``http://www.w3.org/2000/01/rdf-schema#subClassOf``

let nextClass = prop

let PRop =new  RDF.RDFClass.``http://dbpedia.org/ontology/Person``.``http://dbpedia.org/ontology/PersonIndividuals``

let PRoperties = mew RDF.RDFObjectProperty.``http://dbpedia.org/ontology/board``
let p2 = new RDF.RDFObjectProperty.``http://dbpedia.org/ontology/birthPlace``
let p3 = new RDF.RDFObjectProperty.``http://dbpedia.org/ontology/restingPlace``


let element = PRop.``http://dbpedia.org/resource/Alexander_Fleming``.




let enumt = RDF.RDFClass.``http://dbpedia.org/ontology/Person``.``http://dbpedia.org/ontology/PersonIndividuals``

for e in enumt do
    e.ToString

firste.ToString

let ind = aClass.``http://www.w3.org/2000/01/rdf-schema#isDefinedBy``

let prop = aClas


let prop = aClass.``http://dbpedia.org/resource/William_Gibson``

let pp = aClass.``http://www.w3.org/2000/01/rdf-schema#subClassOf``


//let actor = new RDF.RDFClass.``http://dbpedia.org/ontology/Actor``



//let fristind = individualt |> seq.head



//let prop2 = actor.``http://www.w3.org/2000/01/rdf-schema#subClassOf``


//let prop = new RDF.RDFObjectProperty.``http://dbpedia.org/ontology/party``()
//let party = prop.``http://dbpedia.org/ontology/PoliticalParty``


let rdfClass = new RDF.RDFClass.``http://www.biopax.org/release/biopax-level3.owl#Catalysis``.

//let aProperty = new RDF.RDFObjectProperty

//let propOfClass = rdfClass.

//let someClass = new rdfClass.




let propOfPers = new rdfClass.

let property = new RDF.RDFObjectProperty.``http://dbpedia.org/ontology/party``()


let class2 =  property.``http://dbpedia.org/ontology/PoliticalParty``

let nextProp = new class2.GetType



let classviaProp = property.``http://dbpedia.org/ontology/PoliticalParty`` 

type person = RDF.RDFClass.``http://dbpedia.org/ontology/Actor``

let d =  System.Collections.Generic.Dictionary<string,string >()
d.Add("party", "http://dbpedia.org/ontology/party")






let class2 = new rdfObjectProp.name.





type activity = RDF.RDFClass.``http://www.openlinksw.com/schemas/oplweb#Product``





type IConnector =
    abstract member doconnectingstuff : unit -> unit


type LocalConnector (filename:string) =
    interface IConnector with 
        member x.doconnectingstuff () = ()

type RemoteConnector (url:string) =
    interface IConnector with 
        member x.doconnectingstuff () = ()

type RDFLayer(connector:IConnector)=
  

   member x.somemember = connector.



//let d = System.Collections.Generic.Dictionary<string,string >()
//d.Add("name2","Ross")
//let person = RDF.``http://dbpedia.org/ontology/Person``(d).
//person.name

//
//RDF.RdfService.``http://dbpedia.org/ontology/Person`` -- this wouldnt be access directlz ß just for holding tzpes
//
//
//
//let plazers  RDF.BaseBallPlazer >: Seq.toList





 








//let tx = new MSR.RDFTypeProvider.MainRDFType<"http://biocyc.bio2rdf.org/sparql">()



















   // let t = new RDFTypeProvider.RDFTypeProvider.MiniRDF<"http://dbpedia.org/sparql">()










    



    



   
















