module RDFDataStructure

open System
open System.Text
open System.Collections.Generic

[<AutoOpen>]

type RdfGraphType =
    {Name: string
     Id : string }

type RDFClassType =
    {Name: string
     Id: string }

type RDFPropertyType =
    {Name: string
     Id: string}
