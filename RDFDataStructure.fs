namespace Samples.RdfTypeProvider

open System
open System.Text
open System.Collections.Generic

[<AutoOpen>]

type RdfClass() =
    let data = Dictionary<(string*string),Lazy<obj>>()

    static let rec createIndividual (connector:Connector,uri:string) =
        let individual = RdfClass()  
        connector.getPropertiesOfIndWithLiteralRange uri
        |> Seq.groupBy fst
        |> Seq.iter(fun (key,values) -> individual.SetValue((key,""), lazy box( values |> Seq.map snd |> Seq.toList )))

        connector.getPropertiesOfIndWithResourceRange uri
        |> Seq.groupBy (fun (propName,_,_) -> propName)
        |> Seq.iter(fun (propName,values) -> 
            values
            |> Seq.groupBy( fun (_,className,_) -> className )
            |> Seq.iter(fun (className,values) ->
                // this is WRONG!!!  
                individual.SetValue((propName,className),lazy box ( values |> Seq.map( fun (_,_,value) -> (createIndividual(connector,value))) |> Seq.toList ))))

        individual
//                                                                                      individual.SetValue((propName,propClass),lazy box (createIndividual(connector,value))))
    member __.GetValue(key) =
        match data.TryGetValue key with
        | (true,v) -> v.Force()
        | _ -> box []

    member __.SetValue(key,value) =
        if data.ContainsKey key then data.[key] <- value
        else data.Add(key,value)

    static member Create(connector,uri) = createIndividual(connector,uri)        

    override __.ToString() = 
        let sb = System.Text.StringBuilder()
        for kvp in data do sb.AppendFormat("{0} : {1}",kvp.Key,kvp.Value).AppendLine() |> ignore
        sb.ToString()


type RdfGraphType =
    {Name: string
     Id : string }

type RDFClassType =
    {Name: string
     Id: string }

type RDFPropertyType =
    {Name: string
     Id: string}
