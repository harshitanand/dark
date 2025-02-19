/// Standard libraries for Directories
module StdLibCli.Libs.Directory

open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude
open LibExecution.RuntimeTypes

module StdLib = LibExecution.StdLib
open StdLib.Shortcuts

let types : List<BuiltInType> = []

let fns : List<BuiltInFn> =
  [ { name = fn "Directory" "current" 0
      typeParams = []
      parameters = [ Param.make "" TUnit "" ]
      returnType = TString
      description = "Returns the current working directory"
      fn =
        (function
        | _, _, [] ->
          uply {
            let contents = System.IO.Directory.GetCurrentDirectory()
            return DString contents
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }

    { name = fn "Directory" "create" 0
      typeParams = []
      parameters = [ Param.make "path" TString "" ]
      returnType = TResult(TUnit, TString)
      description =
        "Creates a new directory at the specified <param path>. If the directory already exists, no action is taken. Returns a Result type indicating success or failure."
      fn =
        (function
        | _, _, [ DString path ] ->
          uply {
            try
              System.IO.Directory.CreateDirectory(path)
              |> ignore<System.IO.DirectoryInfo>
              return DResult(Ok DUnit)
            with
            | e -> return DResult(Error(DString e.Message))
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "Directory" "delete" 0
      typeParams = []
      parameters = [ Param.make "path" TString "" ]
      returnType = TResult(TUnit, TString)
      description =
        "Deletes the directory at the specified <param path>. If <param recursive> is set to true, it will delete the directory and its contents. If set to false (default), it will only delete an empty directory. Returns a Result type indicating success or failure."
      fn =
        (function
        | _, _, [ DString path ] ->
          uply {
            try
              System.IO.Directory.Delete(path, false)
              return DResult(Ok DUnit)
            with
            | e -> return DResult(Error(DString e.Message))
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "Directory" "list" 0
      typeParams = []
      parameters = [ Param.make "path" TString "" ]
      returnType = TList TString
      description = "Returns the directory at <param path>"
      fn =
        (function
        | _, _, [ DString path ] ->
          uply {
            // TODO make async
            let contents =
              System.IO.Directory.EnumerateFileSystemEntries path |> Seq.toList
            return List.map DString contents |> DList
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated } ]


let contents : StdLib.Contents = (fns, types)
