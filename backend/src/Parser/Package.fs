module Parser.Package

open FSharp.Compiler.Syntax

open Prelude
open Tablecloth

module PT = LibExecution.ProgramTypes

open Utils

type PackageModule = { fns : List<PT.Package.Fn> }

let emptyModule = { fns = [] }


/// Update a CanvasModule by parsing a single F# let binding
/// Depending on the attribute present, this may add a user function, a handler, or a DB
let parseLetBinding
  (modules : List<string>)
  (letBinding : SynBinding)
  : PT.Package.Fn =
  match modules with
  | owner :: modules ->
    let modules = NonEmptyList.ofList modules
    ProgramTypes.PackageFn.fromSynBinding owner modules letBinding
  | _ ->
    Exception.raiseInternal
      "Expected owner, and at least 1 other modules"
      [ "modules", modules; "binding", letBinding ]


let rec parseDecls
  (modules : List<string>)
  (decls : List<SynModuleDecl>)
  : PackageModule =
  List.fold
    emptyModule
    (fun m decl ->
      match decl with
      | SynModuleDecl.Let (_, bindings, _) ->
        let fns = List.map (parseLetBinding modules) bindings
        { m with fns = m.fns @ fns }

      | SynModuleDecl.Types (defns, _) -> List.fold m (fun m d -> m) defns

      | SynModuleDecl.NestedModule (SynComponentInfo (_,
                                                      _,
                                                      _,
                                                      nestedModules,
                                                      _,
                                                      _,
                                                      _,
                                                      _),
                                    _,
                                    nested,
                                    _,
                                    _,
                                    _) ->

        let modules = modules @ (nestedModules |> List.map (fun id -> id.idText))
        let nestedDecls = parseDecls modules nested
        { m with fns = m.fns @ nestedDecls.fns }


      | _ -> Exception.raiseInternal $"Unsupported declaration" [ "decl", decl ])
    decls


let parse (filename : string) (contents : string) : PackageModule =
  match parseAsFSharpSourceFile filename contents with
  | ParsedImplFileInput (_,
                         _,
                         _,
                         _,
                         _,
                         [ SynModuleOrNamespace (_, _, _, decls, _, _, _, _, _) ],
                         _,
                         _,
                         _) ->
    // At the toplevel, the module names will from the filenames
    let names = []
    let modul = parseDecls names decls
    { fns = modul.fns }
  | decl ->
    Exception.raiseInternal "Unsupported Package declaration" [ "decl", decl ]
