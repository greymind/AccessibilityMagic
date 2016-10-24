#light

open System
open System.IO
open System.Xml
open System.Text.RegularExpressions
open FSharp.Data

type TagConfig = {
    tag: string
    attr: string
    empty: string
}

let getTagValue (tagString: string, matchValue: string) =
    let tagAndValue = Regex.Match(matchValue, sprintf "(%s)\s*=\s*['\"]([^'\"]*)['\"]" tagString)
    let valueIfAny =
        match tagAndValue.Success with
            | true -> tagAndValue.Groups |> Seq.cast<Group> |> Seq.map (fun g -> g.Value) |> Seq.skip 2 |> Seq.exactlyOne |> Some
            | false -> None

    valueIfAny

let regex (source, output:string, tags) =
    let path =
        @"C:\pe\platform\apps\employee-details.html"
        //@"C:\pe\platform\src\WebApp\scripts\angular\shared\Forms\PrintForm\Templates\ViewRatingsSummary.cshtml"

    let files = List.ofArray (Directory.GetFiles(source, "*.*html", SearchOption.AllDirectories))

    let getTagNoTag (m: Match) =
        let valueIfAny = getTagValue(tags |> Seq.take 1 |> Seq.exactlyOne, m.Value)

        match valueIfAny with
            | Some _ -> m.Value, valueIfAny.Value
            | None -> m.Value, "NoTag"

    let value path =
        Regex.Matches(File.ReadAllText(path), "<a\s.*/a\s*>")
        |> Seq.cast<Match>
        |> Seq.map getTagNoTag
        |> Seq.toList

    let single = value path
    (single.Length, single) |> ignore

    let getFilename (path:string) =
        path.Replace(source, "")

    let valueIfAny path =
        let matches = value path

        match matches.Length with
            | 0 -> None
            | _ -> Some(getFilename path, matches)

    let valueNoMatterWhat path =
        let matches = value path

        getFilename source, matches

    use file = new StreamWriter(output)

    let data =
        files
        |> Seq.choose valueIfAny
        //|> Seq.map valueNoMatterWhat

    for v in data do
        let path, matches = v

        file.WriteLine(sprintf "%s" path)

        for tag,v in matches do
            file.WriteLine(sprintf ", %s, %s" tag v)

let run =
    regex(@"C:\pe\platform\src\WebApp\scripts", @"C:\Balki\AccessibilityMagic\output.csv", ["href"])

[<EntryPoint>]
let main argv =
    //regex(@"C:\pe\platform\src\WebApp\scripts", @"C:\Balki\AccessibilityMagic\output.csv", ["href"])

    0