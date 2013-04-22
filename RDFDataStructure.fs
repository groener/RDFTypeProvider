namespace Samples.RdfTypeProvider

open System
open System.Text
open System.Collections.Generic

[<AutoOpen>]

type RdfClass() =
    let data = Dictionary<string,Lazy<obj>>()

    static let rec createIndividual (connector:Connector,uri:string) =
        let individual = RdfClass()                                                                                                                                                                                                
        for (key,value) in connector.getPropertiesOfIndWithLiteralRange uri do
            individual.SetValue(key,lazy box value)
        for (key,uri) in connector.getPropertiesOfIndWithResourceRange uri do
            individual.SetValue(key,lazy box (createIndividual(connector,uri)))
        individual

    member __.GetValue(key) =
        match data.TryGetValue key with
        | (true,v) -> v.Force()
        | _ -> box ""

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
