/// Helper functions related to Ops
module LibBackend.Op

open Prelude
open Tablecloth

module PT = LibExecution.ProgramTypes


let eventNameOfOp (op : PT.Op) : string =
  match op with
  | PT.SetHandler _ -> "SetHandler"
  | PT.CreateDB _ -> "CreateDB"
  | PT.RenameDB _ -> "RenameDB"
  | PT.DeleteTL _ -> "DeleteTL"
  | PT.SetFunction _ -> "SetFunction"
  | PT.UndoTL _ -> "UndoTL"
  | PT.RedoTL _ -> "RedoTL"
  | PT.SetExpr _ -> "SetExpr"
  | PT.TLSavepoint _ -> "TLSavepoint"
  | PT.DeleteFunction _ -> "DeleteFunction"
  | PT.SetType _ -> "SetType"
  | PT.DeleteType _ -> "DeleteType"

type RequiredContext =
  | NoContext
  | AllDatastores

/// Returns the 'context', ie. the other stuff on the canvas, that
/// you need to also load in order validate that this op could be added
/// to the oplist/canvas correctly
let requiredContextToValidate (op : PT.Op) : RequiredContext =
  match op with
  | PT.SetHandler _ -> NoContext
  | PT.CreateDB _ -> NoContext
  | PT.SetExpr _ -> NoContext
  | PT.TLSavepoint _ -> NoContext
  | PT.UndoTL _ ->
    // Can undo/redo ops on dbs
    AllDatastores
  | PT.RedoTL _ ->
    // Can undo/redo ops on dbs
    AllDatastores
  | PT.DeleteTL _ -> NoContext
  | PT.SetFunction _ -> NoContext
  | PT.DeleteFunction _ -> NoContext
  | PT.RenameDB _ -> AllDatastores
  | PT.SetType _ -> NoContext
  | PT.DeleteType _ -> NoContext


let requiredContextToValidateOplist (oplist : PT.Oplist) : RequiredContext =
  if List.isEmpty oplist then
    NoContext
  else
    oplist
    |> List.map requiredContextToValidate
    |> List.maxBy (fun requiredContext ->
      match requiredContext with
      | AllDatastores -> 1
      | NoContext -> 0)


let isDeprecated (_op : PT.Op) : bool = false

let hasEffect (op : PT.Op) : bool =
  match op with
  | PT.TLSavepoint _ -> false
  | _ -> true


let tlidOf (op : PT.Op) : tlid =
  match op with
  | PT.SetHandler h -> h.tlid
  | PT.CreateDB (tlid, _, _) -> tlid
  | PT.SetExpr (tlid, _, _) -> tlid
  | PT.TLSavepoint tlid -> tlid
  | PT.UndoTL tlid -> tlid
  | PT.RedoTL tlid -> tlid
  | PT.DeleteTL tlid -> tlid
  | PT.SetFunction f -> f.tlid
  | PT.DeleteFunction tlid -> tlid
  | PT.RenameDB (tlid, _) -> tlid
  | PT.SetType ut -> ut.tlid
  | PT.DeleteType tlid -> tlid


let oplist2TLIDOplists (oplist : PT.Oplist) : PT.TLIDOplists =
  oplist |> List.groupBy tlidOf |> Map.toList

let tlidOplists2oplist (tos : PT.TLIDOplists) : PT.Oplist =
  tos |> List.unzip |> Tuple2.second |> List.concat


let astOf (op : PT.Op) : Option<PT.Expr> =
  match op with
  | PT.SetFunction f -> Some f.body
  | PT.SetExpr (_, _, ast) -> Some ast
  | PT.SetHandler (h) -> Some h.ast
  | PT.CreateDB _
  | PT.DeleteTL _
  | PT.TLSavepoint _
  | PT.UndoTL _
  | PT.RedoTL _
  | PT.DeleteFunction _
  | PT.RenameDB _
  | PT.SetType _
  | PT.DeleteType _ -> None


let withAST (newAST : PT.Expr) (op : PT.Op) =
  match op with
  | PT.SetFunction userfn -> PT.SetFunction { userfn with body = newAST }
  | PT.SetExpr (tlid, id, _) -> PT.SetExpr(tlid, id, newAST)
  | PT.SetHandler (handler) -> PT.SetHandler({ handler with ast = newAST })
  | PT.CreateDB _
  | PT.DeleteTL _
  | PT.TLSavepoint _
  | PT.UndoTL _
  | PT.RedoTL _
  | PT.DeleteFunction _
  | PT.RenameDB (_, _)
  | PT.SetType _
  | PT.DeleteType _ -> op


/// Filter down to only those ops which can be applied out of order
/// without overwriting previous ops' state - eg, if we have
/// SetHandler1 setting a handler's value to "aaa", and then
/// SetHandler2's value is "aa", applying them out of order (SH2,
/// SH1) will result in SH2's update being overwritten
/// NOTE: DO NOT UPDATE WITHOUT UPDATING THE CLIENT-SIDE LIST
let filterOpsReceivedOutOfOrder (ops : PT.Oplist) : PT.Oplist =
  ops
  |> List.filter (fun op ->
    match op with
    | PT.SetHandler _
    | PT.SetFunction _
    | PT.SetType _
    | PT.SetExpr _
    | PT.UndoTL _
    | PT.RedoTL _
    | PT.TLSavepoint _
    | PT.RenameDB _ -> false
    | PT.CreateDB _
    | PT.DeleteTL _
    | PT.DeleteFunction _
    | PT.DeleteType _ -> true)

type AddOpResultV1 =
  { handlers : List<PT.Handler.T> // replace
    deletedHandlers : List<PT.Handler.T> // replace, see note above
    dbs : List<PT.DB.T> // replace
    deletedDBs : List<PT.DB.T> // replace, see note above
    userFunctions : List<PT.UserFunction.T> // replace
    deletedUserFunctions : List<PT.UserFunction.T>
    userTypes : List<PT.UserType.T>
    deletedUserTypes : List<PT.UserType.T> } // replace, see deleted_toplevels

type AddOpParamsV1 = { ops : List<PT.Op>; opCtr : int; clientOpCtrID : string }
