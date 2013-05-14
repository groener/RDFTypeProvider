

module Iamauser=
    let endpoint = new VDS.RDF.Query.SparqlRemoteEndpoint(new Uri(""))
    let t = endpoint |> Connector.ExplicitTypes

    //closure
    let fin arg =
        let i = arg * 2  
        fun x -> x + i 

    let fres = fin 2

    let res = fres 6


    let integers  =
        let counter = ref -1
        fun () -> counter := !counter + 1
                  !counter

    printfn "%A" (integers())
    printfn "%A" (integers())
    printfn "%A" (integers())
    printfn "%A" (integers())

