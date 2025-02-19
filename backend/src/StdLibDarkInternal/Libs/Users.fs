/// StdLib functions for user management. Note that user management is intended to be
/// built in Darklang itself, so this functionality is deliberately sparse.
module StdLibDarkInternal.Libs.Users

open System.Threading.Tasks

open Prelude
open LibExecution.RuntimeTypes
open LibExecution.StdLib.Shortcuts


let typ (name : string) (version : int) : FQTypeName.StdlibTypeName =
  FQTypeName.stdlibTypeName' [ "DarkInternal"; "User" ] name version

let fn (name : string) (version : int) : FQFnName.StdlibFnName =
  FQFnName.stdlibFnName' [ "DarkInternal"; "User" ] name version

// only accessible to the LibBackend.Config.allowedDarkInternalCanvasID canvas
let types : List<BuiltInType> = []

let fns : List<BuiltInFn> =
  [ { name = fn "create" 0
      typeParams = []
      parameters = []
      returnType = TUuid
      description = "Creates a user, and returns their userID."
      fn =
        (function
        | _, _, [] ->
          uply {
            let! canvasID = LibBackend.Account.createUser ()
            return DUuid canvasID
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated } ]

let contents = (fns, types)
