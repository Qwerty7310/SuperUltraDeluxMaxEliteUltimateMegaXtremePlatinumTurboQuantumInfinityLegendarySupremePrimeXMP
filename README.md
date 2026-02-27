# music-player

`music-player` — настольный музыкальный плеер на WPF (.NET 9) для Windows с локальной библиотекой, плейлистами и очередью воспроизведения.

## Возможности

- Добавление локальных аудиофайлов в библиотеку.
- Извлечение метаданных трека (название, артист, альбом, длительность) через `TagLibSharp`.
- Отображение обложки трека (или картинки по умолчанию).
- Воспроизведение, пауза, переход к следующему/предыдущему треку, перемотка, регулировка громкости.
- Очередь воспроизведения.
- Создание плейлистов, добавление/удаление треков в плейлистах.
- Скачивание аудио с YouTube (через `YoutubeExplode`) и добавление в библиотеку.
- Локальное хранение библиотеки в `LiteDB`.
- Сохранение пользовательских настроек (громкость, последний трек).

## Технологии

- `C#`, `WPF`, `.NET 9 (net9.0-windows)`
- `LibVLCSharp` + `VideoLAN.LibVLC.Windows`
- `LiteDB`
- `TagLibSharp`
- `YoutubeExplode`
- `ModernWpfUI`
- `Extended.Wpf.Toolkit`

## Требования

- Windows 10/11
- .NET SDK 9.0+

## Запуск

Из корня репозитория:

```bash
dotnet restore SDUDPMEUHMEPTQILSPX/SDUDPMEUHMEPTQILSPX.csproj
dotnet build SDUDPMEUHMEPTQILSPX/SDUDPMEUHMEPTQILSPX.csproj -c Release
dotnet run --project SDUDPMEUHMEPTQILSPX/SDUDPMEUHMEPTQILSPX.csproj
```

Либо открыть решение `SDUDPMEUHMEPTQILSPX.sln` в Visual Studio и запустить проект.

## Где хранятся данные

- База библиотеки: `%AppData%/SDUDPMEUHMEPTQILSPX/library.db`
- Пользовательские настройки (громкость, последний трек): стандартное хранилище `Properties.Settings` для Windows-приложения.

## Поддерживаемые форматы (импорт из файлов)

`.mp3`, `.wav`, `.aac`, `.m4a`, `.wma`, `.flac`, `.ogg`, `.opus`, `.webm`, `.mid`, `.midi`

## Структура проекта

- `SDUDPMEUHMEPTQILSPX/Views` — окна интерфейса (`MainWindow`, диалоги плейлистов и загрузки).
- `SDUDPMEUHMEPTQILSPX/Player` — модели `Track` и `Playlist`.
- `SDUDPMEUHMEPTQILSPX/Database` — контекст и сущности `LiteDB`.
- `SDUDPMEUHMEPTQILSPX/Assets` — ресурсы (например, обложка по умолчанию).

## Примечания

- Плеер использует `LibVLC`, поэтому воспроизведение зависит от поддержки форматов VLC.
- Для загрузки с YouTube требуется доступ к сети.
