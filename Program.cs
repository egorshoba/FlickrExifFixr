using Newtonsoft.Json;
using ExifLibrary;

class Program
{
    static void Main(string[] args)
    {
        string metadataFolder = "metadata"; // Папка с JSON
        string mediaFolder = "photos";      // Папка с файлами
        bool dryRun = false;                // Флаг: true - только тест, false - обновить метаданные

        var jsonFiles = Directory.GetFiles(metadataFolder, "*.json");

        foreach (var jsonFile in jsonFiles)
        {
            string jsonContent = File.ReadAllText(jsonFile);
            dynamic metadata = JsonConvert.DeserializeObject(jsonContent);

            string id = metadata.id;
            string dateTaken = metadata.date_taken;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(dateTaken))
            {
                Console.WriteLine($"Отсутствуют данные в JSON: {Path.GetFileName(jsonFile)}");
                continue;
            }

            // Поиск файла с указанным ID
            var matchingFiles = Directory.GetFiles(mediaFolder)
                                         .Where(file => Path.GetFileName(file).Contains(id))
                                         .ToList();

            if (matchingFiles.Count == 0)
            {
                Console.WriteLine($"Файл с ID {id} не найден.");
                continue;
            }
            if (matchingFiles.Count > 1)
            {
                Console.WriteLine($"Найдены несколько файлов с ID {id}: {string.Join(", ", matchingFiles)}");
                continue;
            }

            string filePath = matchingFiles.First();
            Console.WriteLine($"Обработка: {filePath}");
            Console.WriteLine($"Дата съёмки: {dateTaken}");

            if (dryRun)
            {
                Console.WriteLine($"[ТЕСТ] Будут обновлены метаданные для {filePath}");
                continue;
            }

            try
            {
                var parsedDate = DateTime.Parse(dateTaken);

                if (IsImageFile(filePath))
                {
                    UpdateExifMetadata(filePath, parsedDate);
                }
                else if (IsVideoFile(filePath))
                {
                    // Для видео можно добавить дополнительные библиотеки, например, TagLib#.
                    Console.WriteLine($"Метаданные для видео не обновляются. Устанавливаем атрибуты файла.");
                }

                // Если формат не поддерживает метаданные, устанавливаем атрибут Created
                File.SetCreationTime(filePath, parsedDate);
                File.SetLastWriteTime(filePath, parsedDate);

                Console.WriteLine($"Метаданные обновлены для {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки {filePath}: {ex.Message}");
            }
        }

        Console.WriteLine("Обработка завершена.");
    }

    static bool IsImageFile(string filePath)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png" };
        return extensions.Contains(Path.GetExtension(filePath).ToLower());
    }

    static bool IsVideoFile(string filePath)
    {
        var extensions = new[] { ".mp4", ".avi", ".mov" };
        return extensions.Contains(Path.GetExtension(filePath).ToLower());
    }

    static void UpdateExifMetadata(string filePath, DateTime dateTaken)
    {
        try
        {
            var imageFile = ImageFile.FromFile(filePath);

            imageFile.Properties[ExifTag.DateTimeOriginal] = new ExifDateTime(ExifTag.DateTimeOriginal, dateTaken);
            imageFile.Properties[ExifTag.DateTimeDigitized] = new ExifDateTime(ExifTag.DateTimeDigitized, dateTaken);

            imageFile.Save(filePath);

            Console.WriteLine($"EXIF данные обновлены для {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обновления EXIF для {filePath}: {ex.Message}");
        }
    }
}