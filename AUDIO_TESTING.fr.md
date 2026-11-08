# Vérification de la capture audio Windows

## Versions de Windows prises en charge

ScreenTools cible Windows 10 et Windows 11 et utilise Windows Core Audio via NAudio pour capturer le son système. La capture en boucle WASAPI récupère directement le périphérique de sortie sélectionné : elle ne nécessite ni Stereo Mix, ni l’activation de Stereo Mix, ni un nom de périphérique d’entrée DirectShow pour FFmpeg.

Le microphone reste une source de capture Windows WaveIn indépendante et utilise le périphérique d’enregistrement Windows par défaut. Le microphone et le son système sont enregistrés dans des fichiers WAV distincts, puis mixés dans le MP4.

**Vérification matérielle effectuée ici :** Windows 11 Famille, version 10.0.26300, 64 bits. Windows 10 fait partie des systèmes ciblés par l’application, mais les essais matériels décrits ici ont été réalisés uniquement sous Windows 11. Une validation sur du matériel Windows 10 reste nécessaire avant la publication.

## Capture et synchronisation

- Le son système utilise le périphérique de sortie Multimedia par défaut de Windows, identifié par son identifiant Core Audio. Le service ne choisit pas arbitrairement un périphérique selon l’ordre alphabétique de son nom.
- Les périphériques actifs sont actualisés avant la capture et à la reprise après une pause. Si le périphérique disparaît entre son énumération et le démarrage, le service réessaie avec le périphérique de sortie par défaut actuel.
- WASAPI écrit le format d’échantillonnage natif du périphérique dans un fichier WAV. Chaque intervalle continu d’enregistrement forme un segment distinct.
- Le microphone utilise le périphérique WaveIn par défaut et écrit un WAV PCM mono, 44,1 kHz, 16 bits. Il est mis en pause et repris avec le chronomètre de l’enregistrement.
- La première image vidéo capturée définit le zéro de la chronologie exportée. Les décalages de début audio sont convertis sur cette chronologie : les décalages positifs sont complétés par du silence et les décalages négatifs sont rognés.
- FFmpeg rééchantillonne l’audio de manière asynchrone, mixe tous les segments disponibles avec `amix` (`duration=longest`, `normalize=1`) et limite le résultat à la durée de la vidéo encodée. Le mixage n’utilise donc pas `-shortest`, qui pourrait tronquer l’enregistrement vidéo si une source audio se terminait plus tôt.
- Le chronomètre s’arrête pendant une pause. WASAPI ferme le segment WAV courant à la pause et en crée un autre à la reprise avec le bon décalage temporel. Les noms et décalages des segments sont inscrits dans `recording.json`.
- Si une source audio ne démarre pas ou échoue pendant la capture, son état passe à indisponible dans le HUD d’enregistrement. Le détail localisé de l’erreur est conservé dans les détails de l’artefact. La capture vidéo, la finalisation et la conservation de la séquence d’images continuent indépendamment.

Une déconnexion en cours de session est signalée ; le service ne bascule pas silencieusement vers une autre sortie au milieu d’un fichier. La vidéo continue. L’utilisateur peut lancer un nouvel enregistrement ou mettre en pause puis reprendre après avoir sélectionné une sortie fonctionnelle.

## Vérification automatisée

Depuis le dossier de la solution :

```powershell
dotnet test .\ScreenTools.sln -c Release
```

La suite xUnit comprend :

- La capture matérielle microphone seul, son système seul et les deux sources ensemble. Les tests système seul et combiné jouent un signal de faible niveau et vérifient la présence d’échantillons non nuls dans la capture WASAPI. Les tests microphone vérifient également des échantillons capturés non nuls.
- La sélection du périphérique de sortie Windows par défaut sans entrée Stereo Mix.
- La création des segments WASAPI après pause/reprise et leurs décalages sur la chronologie.
- L’encodage FFmpeg des modes microphone seul, son système seul et combiné. Les tests couvrent le délai positif et le rognage d’un segment audio qui commence avant la première image vidéo.
- Les échecs simulés au démarrage et en cours de capture audio. Ils vérifient que la vidéo reste active et que l’état audio passe à indisponible.

Les tests matériels s’exécutent lorsqu’un périphérique audio est disponible. Sur une machine sans entrée ou sortie active, le test matériel ne sollicite pas le matériel ; les tests FFmpeg et d’isolation des erreurs restent indépendants du matériel.

## Matériel utilisé pour la vérification

| Élément | Périphérique détecté |
|---|---|
| Système | Windows 11 Famille, 10.0.26300, 64 bits |
| Sortie par défaut | Haut-parleurs (Bose SLIII) |
| Autres sorties actives | DELL S2421HGF / audio écran Intel ; Realtek High Definition Audio ; périphériques Steam Streaming |
| Entrée par défaut | Microphone (Realtek High Definition Audio) |
| Autre entrée active | Microphone (Steam Streaming Microphone) |

Le test matériel a utilisé la sortie Bose par défaut via WASAPI loopback, sans entrée Stereo Mix. WaveIn a utilisé le microphone Realtek par défaut.

## Liste de vérification manuelle

1. Sous Windows 10 et Windows 11, définir les haut-parleurs/casques souhaités comme périphérique de lecture **Multimedia** par défaut et un microphone fonctionnel comme périphérique d’enregistrement par défaut.
2. Stereo Mix peut rester désactivé ou absent. N’activer que les options microphone/son système testées.
3. Enregistrer trois courtes séquences : microphone seul (parler ou taper dans les mains), son système seul (lancer un son connu), puis les deux ensemble.
4. Mettre en pause plusieurs secondes, reprendre, puis vérifier que le MP4 ne contient pas de son pendant la pause et que les deux sources reprennent de manière synchronisée.
5. Examiner `recording.json` et les WAV annexes. Le manifeste indique les sources, les noms de segments et les décalages utilisés pour le mixage MP4.
6. Pour tester une panne, changer ou débrancher la sortie audio pendant un enregistrement. L’état du son système doit passer à indisponible, un détail d’erreur doit apparaître et la vidéo doit continuer jusqu’à la fin.
7. Répéter ces vérifications sur du matériel Windows 10 avant de considérer la validation multi-version comme terminée.
