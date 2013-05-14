module test

#if INTERACTIVE
#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\RDFTypeProvider-git\RDFTypeProvider-git\RDFTypeProvider\bin\Debug\RDFTypeProvider_git.dll"
#endif

import 

open Samples.

type rdf = Samples.RdfTypeProvider.RdfDataProvider< "http://dbpedia.org/sparql", 100>
let dc = rdf.GetDataContext()


let a = dc.``http://dbpedia.org/ontology/ActorCollection``.Individuals.``http://dbpedia.org/resource/Amber_Lynn``

let list = ResizeArray< rdf.RdfService.``http://dbpedia.org/sparql``.``http://dbpedia.org/ontology/Actor`` >()

list.Add a

