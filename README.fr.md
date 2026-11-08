# ScreenTools

**Documentation :** [English](README.md) · [Français](README.fr.md) · [简体中文](README.zh-CN.md)

ScreenTools est une application expérimentale pour Windows qui réunit les captures d’écran, l’historique du presse-papiers, l’enregistrement vidéo et la relecture instantanée dans un outil léger.

> **État du projet :** l’application est fonctionnelle, mais elle n’est pas encore prête pour un usage en production. La matrice matérielle de 15/60 minutes, la validation sous Windows 10, la gestion de plusieurs écrans et l’automatisation des versions doivent encore être exécutées en conditions réelles.

## Fonctionnalités

- Captures d’écran avec raccourcis globaux, choix du format et du dossier de destination, et historique des fichiers récents.
- Gestion de l’historique du presse-papiers.
- Enregistrement de l’écran avec démarrage, pause, reprise, profils de qualité, capture du microphone et export MP4 avec FFmpeg.
- Relecture instantanée avec tampon circulaire limité à 160 Mio, purge selon l’ancienneté des images et export de séquences récentes.
- Encodage des captures avec moins d’allocations : le bitmap capturé est réutilisé lorsque c’est possible et les images redimensionnées passent au format 24 bits avant l’encodage JPEG.
- Paramètres, historique des sorties et historique du presse-papiers enregistrés via des fichiers temporaires puis remplacés atomiquement. Les fichiers JSON endommagés sont conservés séparément pour permettre leur récupération.
- Capture du son système depuis la sortie Windows par défaut via NAudio/WASAPI, sans Stereo Mix. Le microphone et le son système sont horodatés puis mixés lors de l’export MP4.
- Diagnostics d’enregistrement : nombre d’images capturées/ignorées/en échec, octets JPEG écrits et mémoire maximale du processus dans le manifeste.
- Prise en charge DPI Per-Monitor V2, avec un sélecteur de zone par écran et un placement des fenêtres flottantes adapté à chaque écran.
- Projet de création d’un package MSIX pour le Microsoft Store et la distribution par installation manuelle.
- 97 tests xUnit couvrant la persistance, les limites de relecture, la localisation, le cycle de vie et la pause/reprise, la durée MP4 et la continuité audio, le mixage FFmpeg, l’isolation des pannes, la capture réelle du microphone/WASAPI et les sondes physiques de capture sur chaque écran connecté.
- Outil autonome de mesure des performances pour la capture réelle de l’écran et les charges synthétiques du tampon de relecture.

## Points restant à valider

- Exécuter la matrice d’enregistrement physique de 15/60 minutes sous Windows 11 et Windows 10.
- Mesurer la durée des relectures, les pertes d’images, l’utilisation mémoire et les scénarios où l’enregistrement et l’export d’une relecture se chevauchent.
- Terminer la matrice physique multi-écrans/haute résolution DPI (125 %, 150 %, DPI mixtes, dispositions verticales et Windows 10).
- Étendre les tests d’intégration et automatiser la génération des packages et des versions.

Consultez [AUDIO_TESTING.fr.md](AUDIO_TESTING.fr.md) pour l’architecture WASAPI et les tests matériels ; [RECORDING_VALIDATION.fr.md](RECORDING_VALIDATION.fr.md) pour la matrice de stabilité ; [MULTI_MONITOR_VALIDATION.fr.md](MULTI_MONITOR_VALIDATION.fr.md) pour la matrice d’écrans et le script de diagnostic du ticket n° 4 ; [REMAINING_WORK.md](REMAINING_WORK.md) pour la liste des tâches ; et [DESIGN.md](DESIGN.md) pour les règles d’interface.

## Technologies

- C# et WPF avec .NET 8 pour Windows.
- FFmpeg pour l’encodage MP4.
- NAudio pour la capture du son système via WASAPI.
- xUnit pour les tests automatisés.

## Compilation, tests et lancement

Prérequis :

- Windows 10 ou Windows 11.
- SDK .NET 8.
- FFmpeg pour l’export MP4. L’application recherche FFmpeg dans son dossier, dans plusieurs emplacements courants (dont Chocolatey) et dans les entrées fiables de `PATH`.

Exécutez les commandes de la solution depuis le dossier `ScreenTools` afin que les chemins relatifs entre projets soient résolus correctement :

```powershell
Push-Location .\ScreenTools
try {
    dotnet restore .\ScreenTools.sln
    dotnet build .\ScreenTools.sln -c Release
    dotnet test .\ScreenTools.sln -c Release
    dotnet run --project .\ScreenTools\ScreenTools.csproj -c Release
} finally {
    Pop-Location
}
```

### Tests de performance

Lancez l’outil de mesure des performances depuis le dossier de la solution :

```powershell
Push-Location .\ScreenTools
try {
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- capture --iterations 60 --csv ..\BenchmarkResults\capture.csv
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- buffer --iterations 340 --csv ..\BenchmarkResults\buffer.csv
} finally {
    Pop-Location
}
```

Le mode `capture` mesure la capture réelle de l’écran, le redimensionnement et l’encodage JPEG. Le mode `buffer` mesure l’ajout d’images dans le tampon et vérifie sa limite mémoire avec des images synthétiques de 512 Kio. Le mode `all` exécute les deux scénarios. Le fichier CSV est facultatif et les résultats de mesure sont exclus de Git.

Sans FFmpeg, les images capturées peuvent être conservées sous forme de séquence, mais l’export MP4 n’est pas disponible.

## Contribution

À ce stade, améliorer la fiabilité est plus utile que d’ajouter des fonctionnalités. Lisez [CONTRIBUTING.md](CONTRIBUTING.md), consultez les tickets ouverts et discutez des changements architecturaux importants avant de soumettre une modification conséquente.

## Licence

MIT. Consultez [LICENSE](LICENSE).