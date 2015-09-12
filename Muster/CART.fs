﻿namespace Muster.DataStructuresAndAlgorithms


open System
open Muster.Extensions


module CART =


    let operatorErrorMsg (op : string) = op + " operator called with incompatible arguments"


    //[<RequireQualifiedAccess; StructuralComparison; StructuralEquality>]
    type CatType =
        | Int of int
        | Str of string
        | Bool of bool
        static member getDefaultOf s =
            match s with
            | CatType.Int _ -> CatType.Int 0
            | CatType.Str _ -> CatType.Str ""
            | CatType.Bool _ -> CatType.Bool true
        static member (+) (s, t) =
            match s, t with
            | CatType.Int u, CatType.Int v -> CatType.Int(u + v)
            | CatType.Str u, CatType.Str v -> CatType.Str(u + v)
            | CatType.Bool u, CatType.Bool v -> CatType.Bool(u || v)
            | _ -> failwith (operatorErrorMsg "+")
        static member (-) (s, t) =
            match s, t with
            | CatType.Int u, CatType.Int v -> CatType.Int(u - v)
            | _ -> failwith (operatorErrorMsg "-")
        static member (*) (s, t) =
            match s, t with
            | CatType.Int u, CatType.Int v -> CatType.Int(u * v)
            | CatType.Bool u, CatType.Bool v -> CatType.Bool(u && v)
            | _, CatType.Bool u | CatType.Bool u, _ -> if u then s else CatType.getDefaultOf s
            | _ -> failwith (operatorErrorMsg "*")
        static member (/) (s, t) =
            match s, t with
            | CatType.Int u, CatType.Int v -> CatType.Int(u / v)
            | _ -> failwith (operatorErrorMsg "/")


    //[<RequireQualifiedAccess; StructuralComparison; StructuralEquality>]
    type ContType =
        | Flt of float
        static member (+) (s, t) = match s, t with ContType.Flt u, ContType.Flt v -> ContType.Flt (u + v)
        static member (-) (s, t) = match s, t with ContType.Flt u, ContType.Flt v -> ContType.Flt (u - v)
        static member (*) (s, t) = match s, t with ContType.Flt u, ContType.Flt v -> ContType.Flt (u * v)
        static member (/) (s, t) = match s, t with ContType.Flt u, ContType.Flt v -> ContType.Flt (u / v)


    //[<RequireQualifiedAccess; StructuralComparison; StructuralEquality>]
    type DataType =
        | Cat of CatType
        | Cont of ContType
        static member (+) (s, t) =
            match s, t with
            | DataType.Cat u, DataType.Cat v -> DataType.Cat(CatType.op_Addition(u, v))
            | DataType.Cont u, DataType.Cont v -> DataType.Cont(ContType.op_Addition(u, v))
            | _ -> failwith (operatorErrorMsg "+")
        static member (-) (s, t) =
            match s, t with
            | DataType.Cat u, DataType.Cat v -> DataType.Cat(CatType.op_Subtraction(u, v))
            | DataType.Cont u, DataType.Cont v -> DataType.Cont(ContType.op_Subtraction(u, v))
            | _ -> failwith (operatorErrorMsg "-")
        static member (*) (s, t) =
            match s, t with
            | DataType.Cat u, DataType.Cat v -> DataType.Cat(CatType.op_Multiply(u, v))
            | DataType.Cont u, DataType.Cont v -> DataType.Cont(ContType.op_Subtraction(u, v))
            | _ -> failwith (operatorErrorMsg "*")
        static member (/) (s, t) =
            match s, t with
            | DataType.Cat u, DataType.Cat v -> DataType.Cat(CatType.op_Division(u, v))
            | DataType.Cont u, DataType.Cont v -> DataType.Cont(ContType.op_Division(u, v))
            | _ -> failwith (operatorErrorMsg "/")


    type DataTable = list<array<DataType>>


    //[<RequireQualifiedAccess>]
    type InfoGainRes = {SplittingValOpt : option<float>; InfoGain : float}


    //[<RequireQualifiedAccess>]
    type PrunedComponents = {ColName : String; ColVal : DataType; PrunedTable : DataTable}


    //[<RequireQualifiedAccess>]
    type DecisionTreeNode =
        | Leaf of DataType
        | LeafList of list<DataType>
        | Internal of Map<DataType * DataType, DecisionTreeNode>


    let coreImpurityFn<'A when 'A : equality> (classVals : list<'A>) : list<float> =
        let classValsLen = float (List.length classVals)
        classVals
        |> Seq.groupBy (id)
        |> Seq.map (fun (s, t) -> (float (Seq.length t)) / classValsLen)
        |> List.ofSeq


    let entropy (classVals : list<DataType>) : float =
        classVals
        |> coreImpurityFn
        |> List.map (fun s -> s * Math.Log(s, 2.0))
        |> (List.sum >> ((*) (-1.0)))


    let giniIndex (classVals : list<DataType>) : float =
        classVals
        |> coreImpurityFn
        |> List.map (fun s -> s * s)
        |> (List.sum >> ((-) 1.0))


    let classificationError (classVals : list<DataType>) : float =
        classVals
        |> coreImpurityFn
        |> (List.max >> ((-) 1.0))


    let getInfoGainForCatVar
        (tblDat : DataTable)
        (idx : int)
        (impurityFn : (list<DataType> -> float))
        (datSetImpurity : float)
        : InfoGainRes =
        let tblDatLen = float(List.length tblDat)
        tblDat
        |> Seq.groupBy (fun s -> s.[idx])
        |> Seq.map (fun (_, s) ->
            s
            |> Seq.map (fun t -> t.[(Array.length t) - 1])
            |> (fun t -> float(Seq.length t), (impurityFn << List.ofSeq) t)
            |> (fun (t, u) -> t * u))
        |> Seq.sum
        |> (fun s -> {InfoGainRes.SplittingValOpt = None; InfoGainRes.InfoGain = datSetImpurity - (s / tblDatLen)})


    let errorMsgs =
        seq [
            ("emptyLstErrorMsg", "The given list is empty");
            ("catErrorMsg", "Expected a categorical variable but encountered a continuous one");
            ("catStrErrorMsg", "Expected a categorical variable of type string but encountered something else");
            ("contErrorMsg", "Expected a continuous variable but encountered a categorical one")]
        |> Map.ofSeq


    let defFltExtractorFn (sq : seq<DataType>) : seq<float> =
        sq
        |> Seq.map (fun s ->
            match s with
            | DataType.Cont(ContType.Flt t) -> t
            | _ -> failwith errorMsgs.["contErrorMsg"])


    let applyExOp
        (exFn : seq<'A> -> seq<'B>)
        (op : seq<'B> -> 'C)
        (sq : seq<'A>)
        : 'C =
        if Seq.isEmpty sq then failwith errorMsgs.["emptyLstErrorMsg"]
        else sq |> exFn |> op


    let getInfoGainForContVar
        (tblDat : DataTable)
        (idx : int)
        (impurityFn : (list<DataType> -> float))
        (datSetImpurity : float)
        : InfoGainRes =
        let sortedTblDat = tblDat |> List.sortBy (fun s -> s.[idx])
        let sortedTblDatLen = float(List.length sortedTblDat)
        let rowLen = (Array.length << List.head) sortedTblDat
        sortedTblDat
        |> Seq.pairwise
        |> Seq.map (fun (s, t) ->
            DataType.op_Division(
                DataType.op_Addition(s.[idx], t.[idx]),
                DataType.Cont(ContType.Flt 2.0)))
        |> Seq.map (fun s ->
            sortedTblDat
            |> List.partition (fun t -> t.[idx] < s)
            |> (fun (t, u) -> [t; u])
            |> List.map (fun t -> float(List.length t) * impurityFn(t |> List.map (fun u -> u.[rowLen - 1])))
            |> List.sum
            |> (fun t -> applyExOp defFltExtractorFn Seq.head [s], datSetImpurity - (t / sortedTblDatLen)))
        |> (fun s ->
                Seq.fold
                    (fun t (u, v) ->
                        if t.InfoGain > v then t
                        else {InfoGainRes.SplittingValOpt = Some u; InfoGainRes.InfoGain = v})
                    (let (t, u) = Seq.head s in {InfoGainRes.SplittingValOpt = Some t; InfoGainRes.InfoGain = u})
                    (Seq.skip 1 s))


    let getInfoGain
        (tblDat : DataTable)
        (idx : int)
        (impurityFn : (list<DataType> -> float))
        (datSetImpurity : float)
        : InfoGainRes =
        match (List.head tblDat).[idx] with
        | DataType.Cat _ -> getInfoGainForCatVar tblDat idx impurityFn datSetImpurity
        | _ -> getInfoGainForContVar tblDat idx impurityFn datSetImpurity


    let getTblDatSplitsForCatVar (tblDat : DataTable) (idx : int) : list<DataTable> =
        tblDat
        |> Seq.groupBy (fun s -> s.[idx])
        |> Seq.map (snd >> List.ofSeq)
        |> List.ofSeq


    let getTblDatSplitsForContVar
        (tblDat : DataTable)
        (idx : int)
        (infoGainRes : InfoGainRes)
        : list<DataTable> =
        let exFn (sq : seq<array<DataType>>) : seq<bool * array<DataType>> =
            sq
            |> Seq.map (fun s ->
                match s.[idx], infoGainRes.SplittingValOpt with
                | DataType.Cont(ContType.Flt t), Some u -> (t < u), s
                | _ -> failwith errorMsgs.["contErrorMsg"])
        let op (sq : seq<bool * array<DataType>>) : seq<DataTable> =
            sq
            |> List.ofSeq
            |> List.partition (fst)
            |> (fun (s, t) -> [List.map snd s; List.map snd t] |> Seq.ofList)
        (applyExOp exFn op (tblDat |> List.sortBy (fun s -> s.[idx]))) |> List.ofSeq


    let getTblDatSplits
        (tblDat : DataTable)
        (idx : int)
        (infoGainRes : InfoGainRes)
        : list<DataTable> =
        match (List.head tblDat).[idx] with
        | DataType.Cat _ -> getTblDatSplitsForCatVar tblDat idx
        | DataType.Cont _ -> getTblDatSplitsForContVar tblDat idx infoGainRes


    let getPrunedComponentsForCatVar (tblsLst : list<DataTable>) (idx : int) : list<PrunedComponents> =
        let exFn (idx : int) (sqTbl: seq<seq<DataType>>) : seq<string * DataType * seq<seq<DataType>>> =
            if (Seq.isEmpty sqTbl) || (((Seq.skip idx) >> Seq.head >> Seq.length) sqTbl) < 2
            then failwith errorMsgs.["emptyLstErrorMsg"]
            else
                let colName, colVal =
                    let tmp = ((Seq.skip idx) >> Seq.head >> (Seq.take 2)) sqTbl in Seq.head tmp, Seq.last tmp
                match colName, colVal with
                | DataType.Cat(CatType.Str colNameStr), DataType.Cat _ -> seq [colNameStr, colVal, sqTbl]
                | _ -> failwith errorMsgs.["catErrorMsg"]
        let op (sq : seq<string * DataType * seq<seq<DataType>>>) : PrunedComponents =
            let colName, colVal, sqTbl = Seq.head sq
            {
            ColName = colName;
            ColVal = colVal;
            PrunedTable =
                (
                if idx < 1 then sqTbl |> Seq.skip 1
                else
                    seq {
                        yield! (Seq.take idx sqTbl)
                        yield! (Seq.skip(idx + 1) sqTbl)})
                |> ((Seq.map List.ofSeq) >> List.ofSeq)
                |> ListExtensions.transpose
                |> List.map (Array.ofList)}
        tblsLst
        |> List.map ((List.map List.ofArray) >> ListExtensions.transpose)
        |> List.map (Seq.map Seq.ofList)
        |> List.map (applyExOp (exFn idx) op)


    let epsilon = 0.001


    let defSplitStopCriterion (sqTbl : seq<seq<DataType>>) : bool = (Seq.length sqTbl) <= 4


    let getPrunedComponentsForContVar
        (tblsLst : list<DataTable>)
        (idx : int)
        (infoGainRes : InfoGainRes)
        (splitStopCriterion : seq<seq<DataType>> -> bool)
        : list<PrunedComponents> =
        let exFn (idx : int) (transSqTbl : seq<seq<DataType>>) : seq<string * float * seq<seq<DataType>>> =
            if (Seq.isEmpty transSqTbl) || (((Seq.skip idx) >> Seq.head >> Seq.length) transSqTbl) < 2
            then failwith errorMsgs.["emptyLstErrorMsg"]
            else
                let colName, colVal =
                    let tmp = ((Seq.skip idx) >> Seq.head >> (Seq.take 2)) transSqTbl in Seq.head tmp, Seq.last tmp
                match colName, colVal with
                | DataType.Cat(CatType.Str colNameStr), DataType.Cont(ContType.Flt v) ->
                    seq [colNameStr, v, transSqTbl]
                | _ -> failwith errorMsgs.["contErrorMsg"]
        let op
            (idx : int)
            (splitStopCriterion : seq<seq<DataType>> -> bool)
            (sq : seq<string * float * seq<seq<DataType>>>)
            : PrunedComponents =
            let colName, colVal, transSqTbl = Seq.head sq
            {
            ColName = colName;
            ColVal =
                let sv = infoGainRes.SplittingValOpt.Value
                (if colVal < sv then sv - epsilon else sv) |> (DataType.Cont << ContType.Flt);
            PrunedTable =
                let splitStopFlg =
                    transSqTbl
                    |> ((Seq.map List.ofSeq) >> List.ofSeq)
                    |> ListExtensions.transpose
                    |> ((List.map Seq.ofList) >> Seq.ofList)
                    |> splitStopCriterion
                (
                if not splitStopFlg then transSqTbl
                elif idx < 1 then Seq.skip 1 transSqTbl
                else
                    seq {
                        yield! (Seq.take idx transSqTbl)
                        yield! (Seq.skip(idx + 1) transSqTbl)})
                |> Seq.map List.ofSeq
                |> List.ofSeq
                |> ListExtensions.transpose
                |> List.map (Array.ofList)}
        tblsLst
        |> List.map ((List.map List.ofArray) >> ListExtensions.transpose)
        |> List.map (Seq.map Seq.ofList)
        |> List.map (applyExOp (exFn idx) (op idx splitStopCriterion))


    let getPrunedComponents
        (tblsLst : list<DataTable>)
        (idx : int)
        (infoGainRes : InfoGainRes)
        (splitStopCriterionOpt : option<seq<seq<DataType>> -> bool>)
        : list<PrunedComponents> =
        let exFn (idx : int) (tblsSq : seq<DataTable>) : seq<bool * seq<DataTable>> =
            let colType = ((Seq.head << (Seq.skip 1) << Seq.head) tblsSq).[idx]
            match colType with
            | DataType.Cat _ -> seq [true, tblsSq]
            | _ -> seq [false, tblsSq]
        let op (idx : int) (catFlgAndTblsSq : seq<bool * seq<DataTable>>) : list<PrunedComponents> =
            let catFlg, tblsSq = Seq.head catFlgAndTblsSq
            if catFlg then getPrunedComponentsForCatVar (List.ofSeq tblsSq) idx
            else getPrunedComponentsForContVar (List.ofSeq tblsSq) idx infoGainRes splitStopCriterionOpt.Value
        applyExOp (exFn idx) (op idx) tblsLst


    let singleValuedCatTypeLst (lst : list<DataType>) : bool =
        let headVal = List.head lst
        lst
        |> List.filter ((=) headVal)
        |> List.length
        |> ((=) (List.length lst))


    let buildC45
        (tbl : DataTable)
        (impurityFn : list<DataType> -> float)
        (splitStopCriterionOpt : option<seq<seq<DataType>> -> bool>)
        : DecisionTreeNode =
        let rec helper (currTbl : DataTable) : DecisionTreeNode =
            let colHdrs = List.head currTbl
            let currTblDat = List.tail currTbl
            let currTblWidth = colHdrs |> Array.length
            let classVals = currTblDat |> List.map (fun s -> s.[(Array.length s) - 1])
            let headClassVal = List.head classVals
            if singleValuedCatTypeLst classVals then DecisionTreeNode.Leaf headClassVal
            else
                let datSetImpurity = impurityFn classVals
                [0 .. currTblWidth - 2]
                |> List.map (fun s -> (s, getInfoGain currTblDat s impurityFn datSetImpurity))
                |> List.maxBy (fun (s, t) -> t.InfoGain)
                |> (fun (s, t) -> s, t, getTblDatSplits currTblDat s t)
                |> (fun (s, t, u) ->
                    getPrunedComponents (List.map (fun v -> colHdrs :: v) u) s t splitStopCriterionOpt)
                |> List.map (fun s ->
                    (DataType.Cat(CatType.Str s.ColName), s.ColVal),
                    (
                    if s.PrunedTable |> (List.head >> Array.length >> (=) 1) then
                        s.PrunedTable
                        |> List.tail
                        |> List.map (fun s -> Array.get s 0)
                        |> (fun s ->
                            if singleValuedCatTypeLst s then DecisionTreeNode.Leaf(List.head s)
                            else DecisionTreeNode.LeafList s)
                    else helper s.PrunedTable))
                |> (Map.ofSeq >> DecisionTreeNode.Internal)
        helper tbl


    let test () : unit = ()

