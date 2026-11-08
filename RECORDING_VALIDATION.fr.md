# Matrice de validation de la stabilité et de la pause/reprise

Ce document définit la matrice d'acceptation reproductible de l'issue GitHub n° 2. Il distingue les tests automatisés des essais sur matériel réel à effectuer pour chaque version de Windows prise en charge.

## Environnement enregistré pour cette validation

| Élément | Environnement |
|---|---|
| Système utilisé pour les tests automatisés | Windows 11 Famille, build 26300, x64 |
| FFmpeg | Version 9.0.2 essentials |
| FFprobe | Version 9.0.2 essentials |
| Sortie utilisée par les tests audio matériels | Haut-parleurs (Bose SLIII) |
| Microphone utilisé par les tests audio matériels | Microphone (Realtek High Definition Audio) |
| Capture d'écran | Capture du bureau Windows via le service de capture de l'application |

Les tests d'intégration FFmpeg automatisés utilisent une vidéo et des signaux sinusoïdaux générés. Ils ne dépendent pas du microphone ni de la sortie physique indiqués ci-dessus. Les tests audio matériels distincts utilisent WASAPI et WaveIn sur les périphériques réels.

**État multi-version :** la suite automatisée a été exécutée sous Windows 11. Une validation matérielle sous Windows 10 reste obligatoire ; la compatibilité Windows 10 ne doit pas être présentée comme validée matériellement avant cet essai.

## Vérification automatisée

Depuis le dossier de la solution :

```powershell
dotnet test .\ScreenTools.sln -c Release
```

La suite automatisée couvre :

- La création MP4, la présence des flux, le décodage complet et la durée avec FFmpeg/FFprobe lorsqu'ils sont installés.
- Le comportement de la chronologie après pause/reprise avec les quatre combinaisons audio : aucun son, microphone seul, son système seul et les deux sources.
- Les décalages des segments audio autour d'une pause volontaire ; la durée réelle de la pause ne doit pas être ajoutée à la chronologie exportée.
- Une vidéo générée de trois secondes avec un segment audio avant la pause simulée et un autre après la reprise. Le test vérifie les flux vidéo/audio, décode tout le MP4 et mesure l'énergie PCM pour vérifier le silence pendant la pause et la présence de son avant/après.
- Quatre enregistrements successifs avec des dossiers de sortie uniques et aucun espace temporaire propre à un enregistrement restant.
- L'abandon : les deux sources audio s'arrêtent et les fichiers temporaires non publiés sont supprimés.
- Un test de capture continue de 15 secondes avec des charges d'image synthétiques de 64 Kio stockées sur disque, comprenant le nombre d'images, les demandes/échecs, les octets écrits et la mémoire maximale du processus.

Les tests dépendant de FFmpeg sont ignorés si FFmpeg n'est pas disponible. Le test de pause vérifie toujours le décodage complet et le silence pendant l'intervalle lorsque FFmpeg est présent ; les vérifications exactes de durée et de flux sont également exécutées lorsque FFprobe est disponible à côté de FFmpeg. Sur la machine enregistrée, les deux outils sont installés et toutes les assertions sont exécutées. Le script de validation des artefacts nécessite les deux outils.

## Diagnostics d'enregistrement ajoutés

Chaque manifeste contient maintenant :

- `CapturedFrameCount`, `CaptureRequests`, `SkippedFrames` et `CaptureFailures`.
- Le total d'octets JPEG écrits dans la séquence temporaire.
- Les valeurs maximales de mémoire de travail et de mémoire privée du processus, échantillonnées après les captures d'image réussies.

Le détail de l'artefact indique également le débit d'images réellement obtenu, les demandes ignorées, les échecs, la taille JPEG et la mémoire maximale. Les images restent stockées sur disque ; l'application n'accumule pas les images JPEG complètes en RAM.

`SkippedFrames` compte les demandes refusées lorsqu'une image précédente est encore en cours de capture ou lorsque le verrou est occupé. Les retards du Dispatcher qui empêchent l'émission d'un tick du minuteur ne sont pas comptabilisés comme des ticks ignorés ; il faut également comparer le FPS obtenu à la durée et au profil sélectionné.

## Scripts de diagnostic

### Valider les enregistrements sauvegardés

Après avoir enregistré une ou plusieurs vidéos :

```powershell
.\tools\Test-RecordingArtifacts.ps1 -OutputRoot "$env:USERPROFILE\Pictures\LensSnap" -ReportPath .\BenchmarkResults\recording-artifact-validation.csv
```

Le script recherche les manifestes, vérifie les flux et la durée MP4 avec FFprobe, décode chaque MP4 entièrement avec FFmpeg, vérifie les fichiers audio annexes et les séquences JPEG conservées si l'encodage MP4 n'était pas disponible. Il renvoie un code de sortie non nul si un artefact échoue.

### Mesurer la mémoire pendant une longue session

Lancer LensSnap et démarrer un enregistrement, puis dans une autre fenêtre PowerShell :

```powershell
.\tools\Measure-RecordingStability.ps1 -DurationMinutes 15 -SampleIntervalSeconds 2
```

Pour une session plus longue, utiliser `-DurationMinutes 60`. Le script enregistre dans un CSV horodaté la mémoire de travail, la mémoire privée, le temps CPU, le nombre de threads, les handles et la taille de l'espace temporaire. Utiliser `-ProcessId` si plusieurs processus LensSnap sont présents.

## Matrice d'acceptation matérielle manuelle

Exécuter chaque ligne sous Windows 11 puis répéter la même matrice sous Windows 10. Utiliser un SSD local avec suffisamment d'espace libre. Noter le build Windows, la résolution d'écran, le profil choisi, les périphériques audio, les versions FFmpeg/FFprobe et la date du test.

| ID | Scénario | Procédure | Critères de réussite |
|---|---|---|---|
| R01 | Lecture et durée MP4 | Enregistrer 30 secondes sans audio, lire le MP4 dans VLC ou Lecteur Windows Media et exécuter le script de validation | Lecture complète ; flux vidéo décodable ; durée à moins d'une seconde ou 10 % de celle du manifeste |
| R02 | Microphone seul | Activer le microphone, désactiver le son système, parler pendant 30 secondes | Vidéo et audio microphone présents ; aucun flux système requis |
| R03 | Son système seul | Désactiver le microphone, activer le son système, lire un son connu pendant 30 secondes | Vidéo et son système présents sans Stereo Mix |
| R04 | Audio combiné | Activer les deux sources et parler pendant la lecture d'un son système | Les deux sources sont audibles ; MP4 entièrement décodable ; aucune source absente |
| R05 | Continuité pause/reprise | Enregistrer, mettre en pause 10 secondes, reprendre et répéter deux fois ; tester microphone seul, son système seul et les deux | Les pauses sont absentes du média ; l'audio reprend ; les décalages du manifeste n'augmentent que pendant l'enregistrement actif |
| R06 | Sessions répétées | Enregistrer et arrêter 10 clips consécutifs, dont un clip avec pause | Chemins de sortie uniques ; MP4 ou séquences d'images valides ; aucun dossier temporaire propre à un enregistrement |
| R07 | Stabilité 15 minutes | Enregistrer 15 minutes avec le profil et l'audio souhaités en exécutant le moniteur mémoire | Aucun crash, blocage ou arrêt inexpliqué ; mémoire sans croissance continue non bornée ; MP4 final décodable |
| R08 | Stabilité 60 minutes | Répéter pendant 60 minutes sur la machine principale | Aucun crash ; finalisation terminée ; métriques mémoire/CPU/disque et diagnostics conservés |
| R09 | Fermeture de la fenêtre active | Pendant l'enregistrement, appuyer sur Alt+F4 sur la fenêtre de contrôle active | L'enregistrement continue et le HUD apparaît ; aucun enregistrement orphelin invisible |
| R10 | Fermeture du HUD | Pendant l'enregistrement, appuyer sur Alt+F4 sur le HUD | Les commandes d'enregistrement actif réapparaissent ; l'enregistrement n'est pas abandonné silencieusement |
| R11 | Fermeture normale de l'application | Revenir aux paramètres via le HUD, laisser l'enregistrement se finaliser, puis fermer la fenêtre principale | L'enregistrement est finalisé avant la sortie ; périphériques audio libérés ; aucun espace temporaire actif |
| R12 | Arrêt brutal du processus | Dans un test jetable, terminer le processus via le Gestionnaire des tâches pendant l'enregistrement, puis relancer | Aucun MP4 corrompu publié ; données temporaires récupérables après le seuil de nettoyage configuré à 12 heures |

### Interprétation du débit d'images

Le planificateur de capture actuel est volontairement limité à environ 15 demandes de capture par seconde. La configuration actuelle des profils est de 12 FPS pour le profil 720P et de 15 FPS pour les profils 1080P et 1440P, même si les noms affichés des profils indiquent 30/60 FPS. Le manifeste et les diagnostics indiquent la cible réelle du moteur et le débit mesuré ; le texte affiché du profil ne prouve pas une capture à 30/60 FPS. La correction de cet écart entre nom du profil et débit réel doit être suivie séparément de cette matrice de stabilité.

## Fiche de résultats

Pour chaque essai manuel, noter :

- Édition, version et build Windows :
- Machine / écran :
- Profil de qualité :
- Microphone et sortie audio :
- Mode audio :
- Durée d'enregistrement :
- Nombre d'images / FPS obtenu / images ignorées / échecs :
- Mémoire de travail maximale / mémoire privée maximale :
- Durée MP4 / résultat du décodage complet :
- Résultat du silence pendant la pause :
- Nettoyage de l'espace temporaire :
- Réussite / échec et remarques :

## État actuel de l'exécution

- Tests automatisés : exécutés sous Windows 11 avec FFmpeg 9.0.2 et FFprobe 9.0.2.
- Test synthétique pause/silence : vérifie le silence pendant la pause et le son PCM avant et après.
- Matrice physique Windows 11 de stabilité 15/60 minutes : documentée ; nécessite une session d'enregistrement GUI longue et volontaire.
- Matrice matérielle Windows 10 : en attente.
