using Helion.Models;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Collection;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.World.Util;

public static class ModelVerification
{
    public static bool VerifyModelFiles(GameFilesModel filesModel, ArchiveCollection archiveCollection, Logger? log)
    {
        if (!VerifyFileModel(archiveCollection, filesModel.IWad, log))
            return false;

        var fileArchives = archiveCollection.Archives.Where(x => x.ExtractedFrom == null);
        if (fileArchives.Count() != filesModel.Files.Count)
        {
            if (log != null)
                LogExtraLoadedArchives(filesModel, log, fileArchives);

            return false;
        }

        if (filesModel.Files.Any(x => !VerifyFileModel(archiveCollection, x, log)))
            return false;

        return true;
    }

    private static void LogExtraLoadedArchives(GameFilesModel filesModel, Logger log, IEnumerable<Archive> fileArchives)
    {
        List<Archive> extraArchives = new();
        foreach (var archive in fileArchives)
        {
            if (filesModel.Files.Any(x => archive.MD5.Equals(x.MD5)))
                continue;
            extraArchives.Add(archive);
        }

        foreach (var extraArchive in extraArchives)
            log.Error($"Loaded '{Path.GetFileName(extraArchive.OriginalFilePath)}' that is not part of this save.");
    }

    private static bool VerifyFileModel(ArchiveCollection archiveCollection, FileModel fileModel, Logger? log)
    {
        if (fileModel.FileName == null)
        {
            log?.Warn("File in save game was null.");
            return true;
        }

        var archive = archiveCollection.GetArchiveByFileName(fileModel.FileName);
        if (archive == null)
        {
            log?.Error($"Required archive {fileModel.FileName} for this save game is not loaded.");
            return false;
        }

        if (fileModel.MD5 == null)
        {
            log?.Warn("MD5 for file in save game was null.");
            return true;
        }

        if (!fileModel.MD5.Equals(archive.MD5))
        {
            log?.Error($"Required archive {fileModel.FileName} did not match MD5 for save game.");
            log?.Error($"Save MD5: {fileModel.MD5} - Loaded MD5: {archive.MD5}");
            return false;
        }

        return true;
    }
}
