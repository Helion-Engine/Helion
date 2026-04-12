using Helion.Models;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.World.Util;

public enum SaveVerificationResult
{
    Unknown,
    Success,
    DifferentIwad,
    DifferentFiles,
    IncorrectOrder
}

public static class ModelVerification
{
    public static SaveVerificationResult VerifyModelFiles(GameFilesModel filesModel, ArchiveCollection archiveCollection, Logger? log)
    {
        if (!VerifyFileModel(archiveCollection, filesModel.IWad, log))
            return SaveVerificationResult.DifferentIwad;

        var fileArchives = archiveCollection.Archives.Where(x => x.ExtractedFrom == null).ToList();
        if (fileArchives.Count != filesModel.Files.Count)
        {
            if (log == null)
                return SaveVerificationResult.DifferentFiles;

            log.Info($"Save file has {filesModel.Files.Count} but {fileArchives.Count} files are loaded.");
            LogExtraLoadedArchives(filesModel, log, fileArchives);
            LogMissingFiles(filesModel, log, fileArchives);
            return SaveVerificationResult.DifferentFiles;
        }

        if (filesModel.Files.Any(x => !VerifyFileModel(archiveCollection, x, log)))
            return SaveVerificationResult.DifferentFiles;

        if (!VerifyFileOrder(archiveCollection, filesModel, log))
            return SaveVerificationResult.IncorrectOrder;

        return SaveVerificationResult.Success;
    }

    private static bool VerifyFileOrder(ArchiveCollection archiveCollection, GameFilesModel filesModel, Logger? log)
    {
        var archives = archiveCollection.Archives.Where(x => x.ExtractedFrom == null).ToArray();
        for (int i = 0; i < filesModel.Files.Count; i++)
        {
            var archive = archives[i];
            var file = filesModel.Files[i];

            if (!archive.MD5.Equals(file.MD5, StringComparison.Ordinal))
            {
                log?.Error($"File '{file.FileName}' at incorrect order for save. Order must be: {string.Join(", ", filesModel.Files.Select(x => x.FileName))}");
                return false;
            }
        }

        return true;
    }

    private static void LogExtraLoadedArchives(GameFilesModel filesModel, Logger log, IList<Archive> fileArchives)
    {
        foreach (var archive in fileArchives)
        {
            if (filesModel.Files.Any(x => archive.MD5.Equals(x.MD5, StringComparison.Ordinal)))
                continue;
            log.Error($"Loaded '{Path.GetFileName(archive.FullPath)}' that is not part of this save.");
        }
    }

    private static void LogMissingFiles(GameFilesModel filesModel, Logger log, IList<Archive> fileArchives)
    {
        foreach (var file in filesModel.Files)
        {
            if (fileArchives.Any(x => x.MD5.Equals(file.MD5, StringComparison.Ordinal)))
                continue;
            log.Error($"Required archive '{file.FileName}' for this save is not loaded.");
        }
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
            log?.Error($"Required archive '{fileModel.FileName}' for this save is not loaded.");
            return false;
        }

        if (fileModel.MD5 == null)
        {
            log?.Warn("MD5 for file in save game was null.");
            return true;
        }

        if (!fileModel.MD5.Equals(archive.MD5, StringComparison.Ordinal))
        {
            log?.Error($"Required archive {fileModel.FileName} did not match MD5 for save game.");
            log?.Error($"Save MD5: {fileModel.MD5} - Loaded MD5: {archive.MD5}");
            return false;
        }

        return true;
    }
}
