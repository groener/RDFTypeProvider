//#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\fsharp3sample\SampleProviders\Samples.MiniCsvProvider\bin\Debug\Samples.MiniCsvProvider.dll"
//#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\RDFtypeProver-svn1\RDF-TP-gg\MergedWB-based\MiniCSV-essential\CSVProvider\CSVProvider\bin\Debug\CSVProvider.dll"
#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\RDFtypeProver-svn1\SampleProviders\Samples.HelloWorldTypeProvider\bin\Debug\Samples.HelloWorldTypeProvider.dll"


let hp = Samples.HelloWorldTypeProvider.Type1("some Data")

hp.InstanceProperty

hp.InstanceMethod 4












    