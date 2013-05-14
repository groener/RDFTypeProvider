
#r @"C:\Users\groener\Documents\Visual Studio 2012\Projects\fsharp3sample\SampleProviders\Debug\net45\Samples.DataStore.Freebase.dll"

let fb = Samples.DataStore.Freebase.FreebaseData.GetDataContext()

type System.Collections.Generic.IEnumerable<'T> with
    member x.Head = Seq.head x

fb.``Science and Technology``.Chemistry.``Chemical Elements``.Individuals.Aluminium.Discoverer.Individuals.Joe.


fb.``Products and Services``.Automotive.``Automobile Companies``.Individuals.``Aston Martin``.``Board members``.


fb.``Science and Technology``.Chemistry.``Chemical Elements``.Individuals.Aluminium

fb._GetDomainCategoryById 

fb.``Products and Services``.Automotive.``Automobile Makes``.Individuals.``Aston Martin``.
fb.``Science and Technology``.Computers.``Computer Emulators``.Individuals.Ameba



let t = fb.Transportation.Aviation.Aircrafts

type domain-aviation = Samples.DataStore.Freebase.FreebaseData.ServiceTypes.Aviation.Aviation

type aviation-individualts = Samples.DataStore.Freebase.FreebaseData.ServiceTypes.Aviation.Aviation.Aircraft_modelDataIndividuals


type domain-travel = Samples.DataStore.Freebase.FreebaseData.ServiceTypes.Travel.Travel




fb.Architecture.Architecture.ArchitectDataCollection

let fb = Samples.DataStore.Freebase.FreebaseData.GetDataContext()

let architecture = fb.``Special Interests``.Architecture
let xx = fb.``Products and Services``.Automotive.``Automobile Companies``.Individuals





let cc = xx.`Platforms

type xx = Samples.DataStore.Freebase.FreebaseData.ServiceTypes.Automotive

let cc = xx.Automotive.Cargo_bedData




let chemistry = fb.Sports.Sports.Cyclists.Individuals.``Joost Posthuma``

let golf = chemistry.Tennis.``Tennis Players``
let cc = chemistry.``Chemical Bonds``.Individuals.``Bent bond``.Name

let  chemicalElements = fb.``Science and Technology``.Chemistry.``Chemical Elements``

let ceList = chemicalElements.Individuals.Actinium
    



