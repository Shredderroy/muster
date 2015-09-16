﻿namespace Muster.DataStructuresAndAlgorithms


open System
open Muster.Extensions
open Muster.Utils


module RandomForest =


    type Forest = list<DecisionTree.Node>


    [<RequireQualifiedAccess>]
    type SampleSize =
        | Int of int
        | Pct of float


    let errorMsgs =
        seq [
            ("sampleSizeErrorMsg", "Invalid specification of SampleSize")]
        |> Map.ofSeq


    let buildWithParams (tbl : DecisionTree.DataTable) (b : int) (sampleSize: SampleSize) : Forest =
        let sampleSize =
            match sampleSize with
            | SampleSize.Int v -> v
            | SampleSize.Pct p when (p > 0.0) && (p < 1.0) ->
                tbl
                |> List.length
                |> float
                |> ( * ) p
                |> Math.Floor
                |> int
            | _ -> failwith errorMsgs.["sampleSizeErrorMsg"]
        let tblLenPred = (List.length tbl) - 1
        let res =
            (List.init b (fun _ -> Misc.getDistinctRandomIntList 0 tblLenPred sampleSize))
            |> List.map (fun s ->
                s
                |> List.sort
                //
                )
        []


    let build (tbl : DecisionTree.DataTable) : Forest =
        //
        []


    let test () : unit = ()

