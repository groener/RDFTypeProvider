#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\fsharp3sample\SampleProviders\Samples.MiniCsvProvider\bin\Debug\Samples.MiniCsvProvider.dll"


let csv = new Samples.FSharp.MiniCsvProvider.MiniCsv<"test.csv">()

let row1 = csv.Data |> Seq.head
let distance = row1.Distance