#r @"packages\FSharp.Data.2.3.2\lib\net40\FSharp.Data.dll"

open System
open System.IO
open System.Xml
open System.Text.RegularExpressions
open FSharp.Data

let parse (path: string) =
    try
        Some (path, HtmlDocument.Load(path))
    with
        | Exception as ex -> None

let getHref (y: HtmlNode) =
    y.TryGetAttribute("href")
    //|> Option.map (fun w -> file, y.InnerText(), w.Value())
    |> Option.map (fun w -> y.Name(), w.Value())

let href (x: string * HtmlDocument) =
    let fullPath, html = x
    let file = Path.GetFileName fullPath

    let rebuild (x: string * string) =
        let a, b = x
        file, a, b

    html.Descendants["a"]
    |> Seq.choose getHref
    |> Seq.map rebuild
    |> Seq.toList

let single =
   let path =
       //@"C:\pe\platform\apps\employee-details.html"
       @"C:\pe\platform\src\WebApp\scripts\angular\shared\Forms\PrintForm\Templates\ViewRatingsSummary.cshtml"

   let results = parse path

   let links =
       match results with
       | Some r -> href r
       | None -> ["", "", ""]

   links

let double =
    let files = List.ofArray (Directory.GetFiles(@"C:\pe\platform\src\WebApp\scripts", "*.*html", SearchOption.AllDirectories))

    let hrefValueIsEmpty (x: string * string * string) =
        let file, innerText, value = x
        Path.GetFileName file, value = ""

    let data =
        files
        |> List.choose parse
        |> List.collect href
        //|> List.countBy hrefValueIsEmpty

    data
    //data.Length, files.Length

let getTagValue (tagString: string, matchValue: string) =
    let tagAndValue = Regex.Match(matchValue, sprintf "(%s)\s*=\s*['\"]([^'\"]*)['\"]" tagString)
    let valueIfAny =
        match tagAndValue.Success with
            | true -> tagAndValue.Groups |> Seq.cast<Group> |> Seq.map (fun g -> g.Value) |> Seq.skip 2 |> Seq.exactlyOne |> Some
            | false -> None

    valueIfAny

let regex =
    let path =
        @"C:\pe\platform\apps\employee-details.html"
        //@"C:\pe\platform\src\WebApp\scripts\angular\shared\Forms\PrintForm\Templates\ViewRatingsSummary.cshtml"

    let files = List.ofArray (Directory.GetFiles(@"C:\pe\platform\src\WebApp\scripts", "*.*html", SearchOption.AllDirectories))

    let getTagNoTag (m: Match) = 
        let valueIfAny = getTagValue("href", m.Value)
        
        match valueIfAny with
            | Some _ -> m.Value, valueIfAny.Value
            | None -> m.Value, "NoTag"

    let value path =
        Regex.Matches(File.ReadAllText(path), "<a\s.*/a\s*>")
        |> Seq.cast<Match>
        |> Seq.map getTagNoTag
        |> Seq.toList

    let single = value path
    single.Length, single

    let valueIfAny path =
        let matches = value path

        match matches.Length with
            | 0 -> None
            | _ -> Some(Path.GetFileName(path), matches)

    let valueNoMatterWhat path =
        let matches = value path

        path, matches

    use file = new StreamWriter(@"C:\Balki\AccessibilityMagic\output.csv")

    let data = 
        files
        //|> Seq.choose valueIfAny
        |> Seq.map valueNoMatterWhat

    for v in data do
        let path, matches = v

        file.WriteLine(sprintf "%s" path)

        for tag,v in matches do
            file.WriteLine(sprintf ", %s, %s" tag v)
        