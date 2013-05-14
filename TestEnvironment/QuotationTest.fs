//#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\RDFtypeProver-svn1\SampleProviders\Samples.MiniCsvProvider\bin\Debug\Samples.MiniCsvProvider.dll"

open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations


// quotation test
type Analyzer(uri : string) =
    // result.Value does not work -- I don't know why
    // therefore thee following function to remove the variable, i.e.,
    // "?t = http://schema.org/LandmarksOrHistoricalBuildings" becomes "http://schema.org/LandmarksOrHistoricalBuildings"
  member this.getValueOfResult(binding : string) : string =
        let position = binding.IndexOf("=")
        let subbinding = binding.Substring(position + 1)
        subbinding.Trim()

//  get explicit types: use "rdf:type"
     // this function is tested and works
    member this.getExplicitTypes  =
        let results = "SELECT DISTINCT ?t WHERE { ?_s rdf:type ?t } LIMIT 2"
        [results]

        
let analyzer = new Analyzer("test1")

let quoted = <@@ analyzer.getExplicitTypes @@>

let nonquoted = analyzer.getExplicitTypes



let quotedInt = <@ 1 @>

let nonquoted = 1

let n = 1
let quotedId = <@ n @>
let nonQuotedId = n


let inc x = x + 1
let quotedFn = <@ inc 1 @>
let nonQuotedFn = inc 1



let quotedOp = <@ 1 + 1 @>
let nonQuotedOp = 1 + 1


let quotedAnonFun = <@ fun x -> x + 1 @>
let nonQuotedAnonFun = (fun x -> x + 1)

let interpretInt exp =
    match exp with
        | Value (x, typ) when typ = typeof<int> -> printfn "%d" (x :?> int)
        | _ -> printfn "not an int"

interpretInt <@ 1 @>
interpretInt <@ 1 + 1 @>


let expr : Expr<int> = <@ 1 + 1 @>
let expr2 : Expr =  <@@ 1 + 1 @@>


//  Splicing Operators : %%
// Combine literal code quotations (as all the examples above) and expressions that are created programmatically.
//  using %% operator,  it is possible to insert an untyped expression object into an untyped quotation
//    (%% is a prefix operator)

<@ 1 + %expr  @>

<@@ 1 + %%expr2 @@>
