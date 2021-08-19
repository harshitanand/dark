module LibExecution.Ast

open System.Threading.Tasks
open FSharp.Control.Tasks
open FSharpPlus

open Prelude
open VendoredTablecloth
open RuntimeTypes

let rec preTraversal (f : Expr -> Expr) (expr : Expr) : Expr =
  let r = preTraversal f in
  let expr = f expr in

  match expr with
  | EInteger _
  | EBlank _
  | EString _
  | EVariable _
  | EBool _
  | ENull _
  | ECharacter _
  | EFQFnValue _
  | EFloat _ -> expr
  | ELet (id, name, rhs, next) -> ELet(id, name, r rhs, r next)
  | EIf (id, cond, ifexpr, elseexpr) -> EIf(id, r cond, r ifexpr, r elseexpr)
  | EFieldAccess (id, expr, fieldname) -> EFieldAccess(id, r expr, fieldname)
  | EApply (id, name, exprs, inPipe, ster) ->
    EApply(id, name, List.map r exprs, inPipe, ster)
  | ELambda (id, names, expr) -> ELambda(id, names, r expr)
  | EList (id, exprs) -> EList(id, List.map r exprs)
  | EMatch (id, mexpr, pairs) ->
    EMatch(id, r mexpr, List.map (fun (name, expr) -> (name, r expr)) pairs)
  | ERecord (id, fields) ->
    ERecord(id, List.map (fun (name, expr) -> (name, r expr)) fields)
  | EConstructor (id, name, exprs) -> EConstructor(id, name, List.map r exprs)
  | EPartial (id, oldExpr) -> EPartial(id, r oldExpr)
  | EFeatureFlag (id, cond, casea, caseb) ->
    EFeatureFlag(id, r cond, r casea, r caseb)


let rec postTraversal (f : Expr -> Expr) (expr : Expr) : Expr =
  let r = postTraversal f in

  let result =
    match expr with
    | EInteger _
    | EBlank _
    | EString _
    | EVariable _
    | ECharacter _
    | EFQFnValue _
    | EBool _
    | ENull _
    | EFloat _ -> expr
    | ELet (id, name, rhs, next) -> ELet(id, name, r rhs, r next)
    | EApply (id, name, exprs, inPipe, ster) ->
      EApply(id, name, List.map r exprs, inPipe, ster)
    | EIf (id, cond, ifexpr, elseexpr) -> EIf(id, r cond, r ifexpr, r elseexpr)
    | EFieldAccess (id, expr, fieldname) -> EFieldAccess(id, r expr, fieldname)
    | ELambda (id, names, expr) -> ELambda(id, names, r expr)
    | EList (id, exprs) -> EList(id, List.map r exprs)
    | EMatch (id, mexpr, pairs) ->
      EMatch(id, r mexpr, List.map (fun (name, expr) -> (name, r expr)) pairs)
    | ERecord (id, fields) ->
      ERecord(id, List.map (fun (name, expr) -> (name, r expr)) fields)
    | EConstructor (id, name, exprs) -> EConstructor(id, name, List.map r exprs)
    | EPartial (id, oldExpr) -> EPartial(id, r oldExpr)
    | EFeatureFlag (id, cond, casea, caseb) ->
      EFeatureFlag(id, r cond, r casea, r caseb)

  f result
