namespace Samples.RdfTypeProvider

open System
open System.Text
open System.Collections.Generic

[<AutoOpen>]

type RdfClass() =
    let complexData = Dictionary<string,Dictionary<string,Lazy<RdfClass>>>()
    let literalData = Dictionary<(string),string list>()

    static let rec createIndividual (connector:Connector,uri:string) =
        let individual = RdfClass()  
        connector.getPropertiesOfIndWithLiteralRange uri
        |> Seq.groupBy fst
        |> Seq.iter(fun (key,values) -> individual.SetLiteralValue((key), values |> Seq.map snd |> Seq.toList ))

        connector.getPropertiesOfIndWithResourceRange uri
        |> Seq.groupBy (fun (propName,_,_) -> propName)
        |> Seq.iter(fun (propName,values) -> 
            values
            |> Seq.groupBy( fun (_,className,_) -> className )            
            |> Seq.distinctBy fst
            |> Seq.iter(fun (className,values) ->
                let data = lazy (createIndividual(connector,className))
                individual.SetComplexValue(propName,className,data)))

        individual

    member __.GetLiteralValue(key) =
        match literalData.TryGetValue key with
        | (true,v) -> v
        | _ -> []

    member __.GetComplexValues(outerKey) =
        match complexData.TryGetValue outerKey with
        | (true,v) -> [for kvp in v -> kvp.Value.Force() ]
        | _ -> []

    member __.GetComplexValue(outerKey,innerKey) =
        match complexData.TryGetValue outerKey with
        | (true,v) -> 
            match v.TryGetValue innerKey with
            | (true,v) -> v.Force()
            | _ -> RdfClass()
        | _ -> RdfClass()

    member __.SetComplexValue(outerKey,innerKey,value) =
        if not (complexData.ContainsKey outerKey) then complexData.[outerKey] <- new Dictionary<_,_>()
        if complexData.[outerKey].ContainsKey innerKey then complexData.[outerKey].[innerKey] <- value
        else complexData.[outerKey].Add(innerKey,value)
    
    member __.SetLiteralValue(key,value) =
        if literalData.ContainsKey key then literalData.[key] <- value
        else literalData.Add(key,value)

    static member Create(connector,uri) = createIndividual(connector,uri)        

    member __.ForceLazyValues() =
        for kvp in complexData do 
            for kvp in kvp.Value do 
                kvp.Value.Force() |> ignore

    override __.ToString() = 
        let sb = System.Text.StringBuilder()
        for kvp in literalData do sb.AppendFormat("{0} : {1}",kvp.Key,kvp.Value).AppendLine() |> ignore
        for kvp in complexData do 
            sb.AppendFormat("{0} : {1}",kvp.Key,kvp.Value).AppendLine() |> ignore
            for kvp in kvp.Value do
                sb.AppendFormat("\t\t {0} : {1}",kvp.Key,kvp.Value).AppendLine() |> ignore    
        sb.ToString()
