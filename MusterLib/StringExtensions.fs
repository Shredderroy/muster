﻿namespace MusterLib


open System
open System.Text.RegularExpressions


module StringExtensions =


    let genWord (rnd : Random) (wordLen : int) : string =
        [|1 .. wordLen|]
        |> Array.map (fun _ -> (97 + (rnd.Next() % 26)) |> (char >> string))
        |> String.concat ""


    let genSentence (rnd : Random) (numWords : int) (maxWordLen : int) : string =
        [1 .. numWords]
        |> List.map (fun _ -> genWord rnd (rnd.Next() % maxWordLen))
        |> String.concat " "


    let getTokens (str : string) =
        [for s in str.Split([|' '|]) do let t = s.Trim() in if not(String.IsNullOrEmpty t) then yield t]


    let getNGrams (str : string) : list<string> =
        let f = (fun s t -> [t] @ [for u in s -> t + " " + u] @ s)
        List.fold f [] (List.rev (getTokens str))


    let getShingles (str : string) : list<string> =
        let f = (fun s (i, t) -> [t] @ [for u in (Seq.take i s) -> t + " " + u] @ s)
        List.fold f [] (let toks = getTokens str in List.zip [0 .. (List.length toks) - 1] (List.rev toks))


    let getMaximalItems (lst : list<string>) : list<string> =
        let f = (fun s t -> (Seq.tryFind(fun (u : string) -> u.Contains t) s) |> Option.isSome)
        let g = (fun s t u -> not ((f s u) || (f t u)))
        let zLst = Seq.mapi (fun i s -> (i, s)) lst
        [for (s, t) in zLst do if g (Seq.take s lst) (Seq.skip (s + 1) lst) t then yield t]


    let getAllLongestSubstrings (numChrs : int) (ltOrEqFlg : bool) (str : string) : list<string> =
        let rec helper
            (resAcc : list<int * int * int>)
            (currStartIdx : int) (currIdx : int)
            (currAcc : Set<char>) (chrsArr : array<char>) : list<int * int * int> =
            if currIdx = Array.length chrsArr then resAcc
            else
                let bestLen = match resAcc with [] -> 0 | (_, h2, _) :: _ -> h2
                let newAcc = Set.add (Array.get chrsArr currIdx) currAcc
                let newAccLen = Set.count newAcc
                let currLen = currIdx - currStartIdx + 1
                let notOverflow, newMatchAsGoodOrBetter = (newAccLen <= numChrs), (currLen >= bestLen)
                if notOverflow && newMatchAsGoodOrBetter then
                    helper
                        (match resAcc with
                        | (_, h2, _) :: _ when h2 < currLen -> [currStartIdx, currLen, newAccLen]
                        | _ -> ((currStartIdx, currLen, newAccLen) :: resAcc))
                        currStartIdx
                        (currIdx + 1)
                        newAcc
                        chrsArr
                elif not notOverflow then helper resAcc (currStartIdx + 1) (currStartIdx + 1) Set.empty chrsArr
                else helper resAcc currStartIdx (currIdx + 1) newAcc chrsArr
        str.ToCharArray() |> helper [] 0 0 Set.empty
        |> List.filter (fun (_, _, s) -> ltOrEqFlg || (s = numChrs))
        |> List.map (fun (s, t, _) -> str.Substring(s, t))
        |> List.rev


    let compress (str : string) : string = (new Regex(@"\s+")).Replace(str, " ")


    let removeNonAlphaNumChars (str : string) : string = compress((new Regex(@"[^0-9A-Za-z]")).Replace(str, " "))


    let removeApostrophe (str : string) : string = compress(str.Replace('\'', ' '))


    let isInteger (str : string) : bool = (new Regex(@"^[0-9]+$")).IsMatch str


    let isFloat (str : string) : bool = (new Regex(@"(^[0-9]?\.[0-9]+$)|(^[0-9]+)")).IsMatch str

