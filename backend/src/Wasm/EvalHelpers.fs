module Wasm.EvalHelpers

open Prelude
open Tablecloth

open LibExecution.RuntimeTypes

let getStateForEval
  (stdlib : LibExecution.StdLib.Contents)
  (types : List<UserType.T>)
  (fns : List<UserFunction.T>)
  : ExecutionState =
  let packageFns = Map.empty // TODO

  let libraries : Libraries =
    let stdlibFns, stdlibTypes = stdlib

    { stdlibTypes = stdlibTypes |> List.map (fun typ -> typ.name, typ) |> Map

      stdlibFns = stdlibFns |> List.map (fun fn -> fn.name, fn) |> Map

      packageFns = packageFns }

  let program : ProgramContext =
    { canvasID = CanvasID.Empty
      internalFnsAllowed = true
      dbs = Map.empty
      userFns = Map.fromListBy (fun fn -> fn.name) fns
      userTypes = Map.fromListBy (fun typ -> typ.name) types
      secrets = List.empty }

  { libraries = libraries
    tracing = LibExecution.Execution.noTracing Real
    program = program
    test = LibExecution.Execution.noTestContext
    reportException = consoleReporter
    notify = consoleNotifier
    tlid = gid ()
    callstack = Set.empty
    onExecutionPath = true
    executingFnName = None }

/// Any 'loose' exprs in the source are mapped without context of previous/later exprs
/// so, a binding set in one 'let' will be unavailable in the next expr if evaluated one by one.
///
/// e.g. given these separated exprs:
///   - `let x = 1`
///   - `1+1`
///   - `x+2`
/// when evaluating `x+2`, `x` wouldn't normally exist in the symtable.
///
/// So, this fn reduces the exprs to one expr ensuring bindings are available appropriately
///   `let x = 1 in (let _ = 1+1 in x+2)`
///
/// `1+1` here may just as well be something like
///   `WASM.callJSFunction "console.log" ["X is " ++ (Int.toString x)])`
///
/// (There's no way to read the symtable from `state`, otherwise I'd eval expr by expr, and
/// update the symtable as I go.)
///
/// TODO: this would belong better somewhere else, but I don't know where.
let exprsCollapsedIntoOne (exprs : List<Expr>) : Expr =
  exprs
  |> List.rev
  |> List.fold (EUnit(gid ())) (fun agg expr ->
    match agg with
    | EUnit (_) -> expr
    | _ ->
      match expr with
      | ELet (id, lp, expr, _) -> ELet(id, lp, expr, agg)
      | other -> ELet(gid (), LPVariable(gid (), "_"), other, agg))
