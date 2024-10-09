namespace LerUI

module Dialogs =
    open Avalonia
    open Avalonia.Platform.Storage
    open Avalonia.Threading
    open System.Collections.Generic

    let showFileDialog (provider: IStorageProvider) (title:string) (filters: FilePickerFileType list) =

        let options = FilePickerOpenOptions(AllowMultiple = false, Title = title, FileTypeFilter = filters)

        async {

            return!
                Dispatcher.UIThread.InvokeAsync<IReadOnlyList<IStorageFile>>
                    (fun _ ->
                        task {
                            try
                                let! x = provider.OpenFilePickerAsync(options)
                                return x
                            with ex ->
                                printfn "Error: %s" (if ex.InnerException <> null then ex.InnerException.Message else ex.Message)
                                return raise ex
                        })|> Async.AwaitTask
        }

    let showFolderDialog (provider: IStorageProvider) title =
        async {
            let! musicFolder = provider.TryGetWellKnownFolderAsync Platform.Storage.WellKnownFolder.Music |> Async.AwaitTask
            let options = FolderPickerOpenOptions(Title = title, SuggestedStartLocation = musicFolder)

            return!
                Dispatcher.UIThread.InvokeAsync<IReadOnlyList<IStorageFolder>>
                    (fun _ -> provider.OpenFolderPickerAsync(options)) |> Async.AwaitTask
        }
