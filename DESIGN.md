# Visual Design System: Minimalism and Native Productivity / 视觉设计系统指南：极致简约与原生生产力 / Système de design visuel : minimalisme et productivité native

---

## 1. Core Design North Star: The Digital Curator / 核心设计北极星：数字策展人 / Étoile polaire du design : le conservateur numérique

**中文:**
本系统的核心理念是**"数字策展人"**。这意味着界面应当像一座极简主义的现代美术馆：墙板（背景）是安静的，光影（材质）是流动的，所有的注意力都应当被引导至内容本身。

我们通过**有意的不对称布局**和**大面积的留白**来打破传统软件的"格子铺"感。界面不再是功能的堆砌，而是一个具有呼吸感的空间。

**English:**
The core philosophy of this system is **"The Digital Curator"**. This means the interface should be like a minimalist modern art museum: the walls (background) are quiet, the light and shadow (materials) flow, and all attention should be directed to the content itself.

We break the "grid铺" feel of traditional software through **intentional asymmetric layouts** and **large areas of whitespace**. The interface is no longer a pile of functions, but a space with breath.

**Français:**
La philosophie centrale de ce système est **"Le Conservateur Numérique"**. Cela signifie que l'interface doit être comme un musée d'art moderne minimaliste : les murs (arrière-plan) sont calmes, la lumière et l'ombre (matériaux) coulent, et toute l'attention doit être dirigée vers le contenu lui-même.

Nous brisons le sentiment de "grille" des logiciels traditionnels grâce à des **dispositions asymétriques intentionnelles** et de **grandes zones d'espace blanc**. L'interface n'est plus un tas de fonctions, mais un espace respirant.

---

## 2. Color Philosophy: Tone and Breath / 色彩哲学：影调与呼吸 / Philosophie des couleurs : ton et respiration

**中文:**
本系统严禁使用纯黑（#000000）或高饱和度的装饰色。我们依靠中性色的微妙推移来建立秩序。

### 调色板逻辑 / Palette Logic / Logique de palette
- **Primary (#005eb1):** 仅用于核心操作点缀，如焦点状态或关键动作。/ Only used for core action accents, such as focus state or key actions. / Uniquement utilisé pour les accents d'action principaux, tels que l'état de focus ou les actions clés.
- **Surface 体系:** 界面深度的核心。通过 `surface-container-lowest` 到 `highest` 的切换实现逻辑分层。/ Core of interface depth. Logical layering achieved through switching between `surface-container-lowest` and `highest`. / Cœur de la profondeur de l'interface. La couche logique est obtenue par commutation entre `surface-container-lowest` et `highest`.
- **Neutrality:** 整体基调维持在灰度区间，确保用户能够长时间专注而不产生视觉疲劳。/ Overall基调 maintained in grayscale to ensure users can focus for long periods without visual fatigue. / Le ton général est maintenu dans les niveaux de gris pour garantir que les utilisateurs peuvent se concentrer longtemps sans fatigue visuelle.

### "无边框"法则 (The No-Line Rule) / "Règle sans bordure"
**禁止使用 1px 实线边框进行区域分割。/ Prohibited from using 1px solid borders for area division./ Interdiction d'utiliser des bordures solides de 1px pour la division de zone.**

边界必须仅通过背景色的阶梯式位移来定义。例如：
- 在 `surface` (基础背景) 上放置 `surface-container-low` 的侧边栏。/ Place a `surface-container-low` sidebar on `surface` (base background). / Placer une barre latérale `surface-container-low` sur `surface` (arrière-plan de base).
- 在 `surface-container-low` 上放置 `surface-container-lowest` 的工作卡片。/ Place `surface-container-lowest` work cards on `surface-container-low`. / Placer des cartes de travail `surface-container-lowest` sur `surface-container-low`.

### 材质与毛玻璃 (Glass & Mica) / Matériaux et verre
为了赋予系统"灵魂"，在全局背景或悬浮层使用**云母 (Mica)** 效果。/ To give the system "soul," use **Mica** effect on global backgrounds或悬浮层./ Pour donner une "âme" au système, utilisez l'effet **Mica** sur les arrière-plans globaux ou les couches flottantes.

- **Token 应用:** `surface` 或 `surface-variant` 配合 80%-90% 的不透明度，并开启 `backdrop-blur` (30px+)。/ **Token application:** `surface` or `surface-variant` with 80-90% opacity, with `backdrop-blur` (30px+) enabled. / **Application de jeton :** `surface` ou `surface-variant` avec 80-90 % d'opacité, avec `backdrop-blur` (30px+) activé.
- 这能让桌面壁纸的色调隐约透出，使应用感觉像是系统原生的一部分，而非孤立的矩形框。/ This allows desktop wallpaper tones to show through slightly, making the app feel like a native part of the system rather than an isolated rectangle./ Cela permet aux tons du fond d'écran de transparaître légèrement, faisant en sorte que l'application semble faire partie intégrante du système plutôt qu'un rectangle isolé.

---

## 3. Typography System: Precise Hierarchy / 字体系统：精密的层次 / Système typographique : hiérarchie précise

**中文:**
我们采用 **Segoe UI** (西文) 与 **微软雅黑** (中文) 的组合。为了体现高端感，我们拉大了 Display 与 Body 之间的字号反差。

| 等级 / Level / Niveau | Token | 尺寸 / Size / Taille | 用途 / Usage / Utilisation |
| :--- | :--- | :--- | :--- |
| **Display-LG** | `display-lg` | 3.5rem | 极少使用的统计数字或欢迎语 / Rarely used statistical numbers or welcome messages. / Chiffres statistiques rarement utilisés ou messages de bienvenue. |
| **Headline-SM** | `headline-sm` | 1.5rem | 主要功能模块的标题 / Titles of main feature modules. / Titres des modules de fonctionnalités principaux. |
| **Title-MD** | `title-md` | 1.125rem | 逻辑组、设置项的分类标题 / Category titles for logic groups or settings items. / Titres de catégorie pour les groupes logiques ou les éléments de paramètres. |
| **Body-MD** | `body-md` | 0.875rem | 默认文本、主要输入内容 / Default text, main input content. / Texte par défaut, contenu de saisie principal. |
| **Label-SM** | `label-sm` | 0.6875rem | 辅助说明、脚注、微小元数据 / Auxiliary instructions, footnotes, tiny metadata. / Instructions auxiliaires, notes de bas de page, petites métadonnées. |

**排版准则 / Typography Principles / Principes typographiques:**
- 标题应使用更重的字重，并增加字母间距。/ Titles should use heavier font weight and increased letter spacing. / Les titres doivent utiliser une poids de police plus lourd et un espacement des lettres accru.
- 正文保持足够的行高，确保长文本阅读的舒适度。/ Body text should maintain sufficient line height for comfortable long-form reading. / Le texte courant doit conserver une hauteur de ligne suffisante pour une lecture confortable de longs textes.

---

## 4. Advanced Depth and Lighting / 高级深度与光影 / Profondeur avancée et éclairage

**中文:**
我们通过"影调叠层"而非物理结构来实现层级。

### 叠层原理 (The Layering Principle) / Principe de superposition
- **底层 (Base):** `surface` / Base layer: `surface`. / Couche de base : `surface`.
- **中层 (Section):** `surface-container-low` (无阴影) / Middle layer: `surface-container-low` (no shadow). / Couche intermédiaire : `surface-container-low` (pas d'ombre).
- **顶层 (Floating/Modal):** `surface-container-lowest` + **环境阴影** / Top layer: `surface-container-lowest` + **ambient shadow**. / Couche supérieure : `surface-container-lowest` + **ombre ambiante**.

### 环境阴影 (Ambient Shadows) / Ombres ambiantes
当组件必须"浮起"时，严禁使用深灰色硬阴影。/ When components must "float," hard dark gray shadows are strictly prohibited./ Lorsque les composants doivent "flotter", les ombres dures gris foncé sont strictement interdites.
- **规范:** 阴影颜色应使用 `on-surface` 颜色的 4%-8% 透明度。/ **Standard:** Shadow color should use 4-8% opacity of `on-surface` color. / **Norme :** La couleur de l'ombre doit utiliser 4 à 8 % d'opacité de la couleur `on-surface`.
- **模糊:** 扩散值应设定在 20px-40px 之间，创造出一种类似自然光照射在柔光纸上的效果。/ **Blur:** Diffusion value should be set between 20px-40px, creating an effect like natural light照射在柔光纸上./ **Flou :** La valeur de diffusion doit être comprise entre 20 et 40 px, créant un effet similaire à la lumière naturelle照射 sur du papier doux.

### 幽灵边框 (Ghost Border) / Bordure fantôme
如果为了无障碍或极高对比度需求必须使用边框，请使用 `outline-variant` 并将不透明度调至 **10%-20%**。视觉上它应当几乎不可见，仅在近距离观察时起到界定作用。/ If borders are required for accessibility or extremely high contrast needs, use `outline-variant` at **10-20% opacity**. Visually it should be almost invisible, serving as a boundary only upon close inspection./ Si des bordures sont requises pour l'accessibilité ou des besoins de contraste extrêmement élevé, utilisez `outline-variant` à **10-20 % d'opacité**. Visuellement, elle doit être presque invisible, servant de frontière uniquement lors d'un examen rapproché.

---

## 5. Component Design Specifications / 组件设计规范 / Spécifications de conception des composants

### Buttons / 按钮
- **Primary:** 背景 `primary`，文字 `on-primary`。圆角 `DEFAULT` (4px)。/ Background `primary`, text `on-primary`. Corner radius `DEFAULT` (4px). / Arrière-plan `primary`, texte `on-primary`. Rayon de coin `DEFAULT` (4px).
- **Secondary:** 背景 `secondary-container`，无边框。/ Background `secondary-container`, borderless./ Arrière-plan `secondary-container`, sans bordure.
- **Interaction:** 悬浮时背景加深 5%，点击时缩放至 0.98x，产生即时物理反馈。/ On hover, background darkens 5%; on click, scale to 0.98x for instant physical feedback./ Au survol, l'arrière-plan s'assombrit de 5 % ; au clic, mise à l'échelle à 0,98x pour un retour physique immédiat.

### Input Fields / 输入字段 / Champs de saisie
- **形态:** 底部 2px 的 `outline-variant` 线条，或全填充的 `surface-container-high` 容器。/ **Form:** Bottom 2px `outline-variant` line, or fully filled `surface-container-high` container./ **Forme :** Ligne `outline-variant` de 2 px en bas, ou conteneur `surface-container-high` entièrement rempli.
- **状态:** 聚焦时，底部线条变为 `primary` 色，且容器背景轻微提亮。/ **State:** On focus, the bottom line becomes `primary` color and the container background is slightly highlighted./ **État :** Au focus, la ligne inférieure devient de couleur `primary` et l'arrière-plan du conteneur est légèrement mis en évidence.

### Lists and Cards / 列表与卡片 / Listes et cartes
- **严禁使用分割线。/ Strictly prohibited from using dividers./ Strictement interdit d'utiliser des séparateurs.**
- 使用 `spacing.4` (0.9rem) 的垂直留白来区分内容项。/ Use `spacing.4` (0.9rem) vertical whitespace to separate content items./ Utilisez un espace blanc vertical `spacing.4` (0,9 rem) pour séparer les éléments de contenu.
- 选中的列表项使用 `surface-container-highest` 背景，并配合左侧 3px 宽的 `primary` 垂直指示条。/ Selected list items use `surface-container-highest` background with a 3px `primary` vertical indicator on the left./ Les éléments de liste sélectionnés utilisent un arrière-plan `surface-container-highest` avec un indicateur vertical `primary` de 3 px à gauche.

### Interaction Response / 交互响应 / Réponse d'interaction
- 去除所有超过 200ms 的过场动画。/ Remove all transition animations longer than 200ms./ Supprimez toutes les animations de transition supérieures à 200 ms.
- 使用 **Cubic-bezier(0, 0, 0, 1)** 的减速曲线，让界面切换显得干脆、专业。/ Use **Cubic-bezier(0, 0, 0, 1)** deceleration curve for crisp, professional interface transitions./ Utilisez la courbe de décélération **Cubic-bezier(0, 0, 0, 1)** pour des transitions d'interface nettes et professionnelles.

---

## 6. Do's and Don'ts / 推荐与禁止 / À faire et à ne pas faire

### ✅ 推荐做法 (Do) / Recommandé
- **保持本地感:** 移除任何涉及"登录"、"同步"、"个人资料"的图标。/ **Maintain local feel:** Remove any icons involving "login," "sync," or "profile." / **Maintenir le sentiment local :** Supprimez les icônes impliquant "connexion", "synchronisation" ou "profil".
- **负空间平衡:** 增加左右页边距，让内容在屏幕中心"呼吸"。/ **Negative space balance:** Increase left/right margins to let content "breathe" in the center of the screen./ **Équilibre de l'espace négatif :** Augmentez les marges gauche/droite pour laisser le contenu "respirer" au centre de l'écran.
- **单色图标:** 仅使用线框风格的单色图标，粗细应与文字字重匹配。/ **Monochrome icons:** Only use line-style single-color icons; weight should match text weight./ **Icônes monochromes :** Utilisez uniquement des icônes monochromes de style ligne ; le poids doit correspondre au poids du texte.

### ❌ 严禁行为 (Don't) / Interdit
- **严禁大圆角:** 超过 12px 的圆角会显得过于"玩具化"，违背工具的专业感。/ **Strictly prohibit large border radius:** Radius over 12px appears too "toy-like," violating the tool's professionalism./ **Interdire les grands rayons de coin :** Un rayon supérieur à 12 px semble trop "jouet", violant le professionnalisme de l'outil.
- **严禁高饱和度:** 除非是极其严重的错误警告，否则不要使用红色、黄色等高亮色。/ **Strictly prohibit high saturation:** Do not use red, yellow, or other highlight colors unless for extremely severe error warnings./ **Interdire la saturation élevée :** N'utilisez pas de rouge, de jaune ou d'autres couleurs de mise en évidence sauf pour des avertissements d'erreur extrêmement graves.
- **严禁分割线堆砌:** 如果一个页面出现了超过 3 条横向分割线，请重新审视你的空间布局。/ **Strictly prohibit divider clutter:** If a page has more than 3 horizontal dividers, re-examine your spatial layout./ **Interdire l'encombrement de séparateurs :** Si une page comporte plus de 3 séparateurs horizontaux, réexaminez votre disposition spatiale.

---

## 7. Conclusion / 结语 / Conclusion

**中文:**
这个系统不是为了"美化"而存在，而是为了"退后"。当用户开始工作时，界面应当消失，只剩下高效的生产力流。请记住：**最好的设计是察觉不到设计的存在。**

**English:**
This system does not exist to "beautify," but to "recede." When the user starts working, the interface should disappear, leaving only an efficient productivity flow. Remember: **the best design is design that is imperceptible.**

**Français:**
Ce système n'existe pas pour "embellir", mais pour "reculer". Lorsque l'utilisateur commence à travailler, l'interface doit disparaître, ne laissant qu'un flux de productivité efficace. Rappelez-vous : **le meilleur design est un design imperceptible.**

---

## 8. Implementation Standards for ScreenTools / 面向 ScreenTools 的落地规范 / Normes de mise en œuvre pour ScreenTools

This section is not abstract aesthetics; it is a direct implementation standard for the current project. The goal is clear: **Make ScreenTools look like a high-end, restrained, native desktop tool, not a loose collection of functional panels.** / 这一节不是抽象审美，而是针对当前项目的直接落地标准。目标很明确：**让 ScreenTools 看起来像一个高端、克制、原生的桌面工具，而不是一组松散的功能面板。** / Cette section n'est pas une esthétique abstraite ; c'est une norme de mise en œuvre directe pour le projet actuel. L'objectif est clair : **Faire en sorte que ScreenTools ressemble à un outil de bureau haut de gamme, sobre et natif, et non à un ensemble lâche de panneaux fonctionnels.**

### 8.1 Product Tone Keywords / 产品气质关键词 / Mots-clés d'ambiance produit
- **Restrained / 克制:** No color堆砌, no effect堆砌, no status堆砌。/ Pas de tas de couleurs, d'effets ou d'états. / Pas de entassement de couleurs, d'effets ou d'états.
- **Precise / 精确:** Alignment, spacing, icon size, and text hierarchy must be unified./ 对齐、间距、图标尺寸、文字层级必须统一。/ Alignement, espacement, taille d'icônes et hiérarchie de texte doivent être unifiés.
- **Lightweight / 轻量:** The interface should float on the desktop like a native tool, not a heavy application container./ 界面应像浮在桌面上的原生工具，而不是厚重的应用容器。/ L'interface doit flotter sur le bureau comme un outil natif, pas un conteneur d'application lourd.
- **Operational / 高频友好:** High-frequency actions take priority; low-frequency settings recede./ 高频动作优先突出，低频设置自动退后。/ Les actions à haute fréquence sont prioritaires ; les paramètres à faible fréquence se replient.

### 8.2 Three-Layer Visual Model / 三层视觉模型 / Modèle visuel à trois couches
All ScreenTools interfaces must strictly adhere to a three-layer structure; no fourth or fifth layers are allowed for free play. / ScreenTools 所有界面必须强制遵守三层结构，不允许自由发挥出第四层、第五层。/ Toutes les interfaces ScreenTools doivent strictement adhérer à une structure à trois couches ; aucune quatrième ou cinquième couche n'est autorisée pour la libre expression.

1. **Host Layer / 宿主层 / Couche hôte**
   - Transparent or nearly transparent. / 透明或近透明./ Transparent ou presque transparent.
   - Does not carry information. / 不承载信息./ Ne porte pas d'informations.
   - Large solid color backgrounds are not allowed. / 不允许出现大面积纯色背景板./ Les grands panneaux de couleur unie ne sont pas autorisés.

2. **Functional Floating Layer / 功能浮层 / Couche flottante fonctionnelle**
   - Carries real content. / 承载真正内容./ Porte le vrai contenu.
   - Uses semi-transparent white or very light neutral colors. / 使用半透明白或非常浅的中性色./ Utilise du blanc semi-transparent ou des couleurs neutres très claires.
   - Shadows should be light; boundaries should be soft. / 阴影要轻，边界要软./ Les ombres doivent être légères ; les limites doivent être douces.

3. **Emphasis Layer / 强调层 / Couche d'accentuation**
   - Used only for current selection state, primary buttons, active switches, and core action prompts. / 仅用于当前选中态、主按钮、激活开关、核心动作提示。/ Utilisé uniquement pour l'état de sélection actuel, les boutons principaux, les commutateurs actifs et les invites d'action principales.
   - The accent color defaults to `#005EB1`. / 强调色默认统一使用 `#005EB1`./ La couleur d'accentuation par défaut est `#005EB1`.

### 8.3 Color Tokens / 颜色 Token / Jetons de couleur

#### Core / 核心
- `Primary`: `#005EB1` / `Primary-active`: `#00529B`
- `Text-primary`: `#2D3435` / `Text-secondary`: `#4F5658` / `Icon-secondary`: `#5F6B7A`

#### Surface / 表面
- `Surface-page`: `#F9F9F9` / `Surface-card`: `#F2F4F4` / `Surface-chip`: `#EEF1F2`
- `Surface-glass`: `#F0FFFFFF` / `Surface-panel-glass`: `#F2FFFFFF`

#### Utility / 实用
- `Divider-soft`: 10%-15% opacity neutral / 10%-15% 不透明度中性色 / 10-15 % d'opacité neutre
- `Shadow-ambient`: `#220F172A`
- `Track-off`: `#B3B9BA`

### 8.4 Color Usage Rules / 颜色使用规则 / Règles d'utilisation des couleurs
- The only color allowed to appear with high recognition on a page is blue. / 页面中唯一允许高识别度出现的颜色是蓝色。/ La seule couleur autorisée à apparaître avec une reconnaissance élevée sur une page est le bleu.
- Red is used only for strong state prompts such as "recording in progress." / 红色只用于"正在录制"这类强状态提示。/ Le rouge n'est utilisé que pour des invites d'état fortes telles que "enregistrement en cours".
- Meaningless decorative colors are not allowed. / 不允许使用无意义的装饰色。/ Les couleurs décoratives dénuées de sens ne sont pas autorisées.
- No more than 1 primary accent color and 1 danger color should appear on the same screen. / 同一屏内不应同时出现超过 1 个主强调色和 1 个危险色。/ Pas plus de 1 couleur d'accentuation principale et 1 couleur de danger ne doivent apparaître sur le même écran.

### 8.5 Font Hierarchy / 字体层级 / Hiérarchie des polices

#### Settings Page / 设置页 / Page de paramètres
- Page title: `24px / SemiBold` / 页面标题：`24px / SemiBold` / Titre de la page : `24px / SemiBold`
- Group title: `16px / Medium` / 分组标题：`16px / Medium` / Titre de groupe : `16px / Medium`
- Setting item title: `16px / Regular` / 设置项标题：`16px / Regular` / Titre de l'élément de paramètre : `16px / Regular`
- Setting item description: `15px-16px / Regular` / 设置项说明：`15px-16px / Regular` / Description de l'élément de paramètre : `15px-16px / Regular`
- Shortcut label: `12px / Mono` / 快捷键标签：`12px / Mono` / Étiquette de raccourci : `12px / Mono`

#### HUD / Floating Bar / HUD / Barre flottante
- Brand text: `14px / Bold` / 品牌字：`14px / Bold` / Texte de marque : `14px / Bold`
- Action label: `14px / Medium` / 操作标签：`14px / Medium` / Étiquette d'action : `14px / Medium`
- Toolbar label: `10px / Medium / uppercase` / 工具栏标签：`10px / Medium / uppercase` / Étiquette de barre d'outils : `10px / Medium / uppercase`
- Timer: `56px-58px / SemiBold` / 计时器：`56px-58px / SemiBold` / Minuterie : `56px-58px / SemiBold`

### 8.6 Font Usage Principles / 字体使用原则 / Principes d'utilisation des polices
- Chinese text should not all use the same font weight. / 中文不要所有文字都用相同字重。/ Le texte chinois ne doit pas tous utiliser le même poids de police.
- Titles should be more stable; descriptions should be lighter. / 标题更稳，说明更轻。/ Les titres doivent être plus stables ; les descriptions doivent être plus légères.
- Numeric information should be stronger, especially timers, durations, and resolutions. / 数字信息要更强，特别是计时器、时长、分辨率。/ Les informations numériques doivent être plus fortes, en particulier les minuteries, les durées et les résolutions.
- Small labels and auxiliary text should主动退后, not抢主操作视觉。/ Small labels and auxiliary text should recede on their own, not抢 the main action visual./ Les petites étiquettes et le texte auxiliaire doivent se replier d'eux-mêmes, sans voler le visuel de l'action principale.

### 8.7 Corner Radius System / 圆角系统 / Système de rayons de coin
- Small buttons / chips: `4px` / 小按钮 / chip：`4px` / Petits boutons / puces : `4px`
- Medium cards: `8px` / 中等卡片：`8px` / Cartes moyennes : `8px`
- Floating layers / toolbars: `12px-16px` / 浮层 / 工具栏：`12px-16px` / Couches flottantes / barres d'outils : `12px-16px`
- Large primary cards: `16px` / 大型主卡片：`16px` / Grandes cartes principales : `16px`
- Corner radius over `16px` is not allowed unless for switches, badges, or circular buttons. / 不允许出现超过 `16px` 的圆角，除非是开关、徽标、圆形按钮。/ Un rayon de coin supérieur à `16px` n'est pas autorisé sauf pour les commutateurs, badges ou boutons circulaires.

### 8.8 Shadow System / 阴影系统 / Système d'ombres
- All floating layers share one ambient shadow system. / 所有浮层共用一套环境阴影。/ Toutes les couches flottantes partagent un système d'ombre ambiante.
- Shadows should not be gray or dirty, and should not become "squishy" skeuomorphic. / 阴影不应发灰发脏，不应"糊"成拟态。/ Les ombres ne doivent pas être grises ou sale, et ne doivent pas devenir "pâteuses" skeuomorphes.
- Recommendation: / 推荐： / Recommandation :
  - `Blur`: `20-28` / `Depth`: `4-8` / `Color`: dark low opacity / `Blur` : `20-28` / `Profondeur` : `4-8` / `Couleur` : opacité sombre faible

### 8.9 Icon Specifications / 图标规范 / Spécifications des icônes
- Use monochrome line icons consistently. / 统一使用单色线性图标。/ Utilisez des icônes lignes monochromes de manière cohérente.
- Weight should remain consistent. / 粗细保持一致./ Le poids doit rester cohérent.
- Default to secondary gray; no fancy multi-color icons. / 默认使用次级灰，不使用花哨多色图标。/ Par défaut, utilisez le gris secondaire ; pas d'icônes multi-couleurs fantaisistes.
- Only the active state is allowed to switch to `Primary`. / 只有当前激活状态才允许转为 `Primary`./ Seul l'état actif est autorisé à basculer vers `Primary`.
- In the same toolbar, icon visual weight must be consistent; no some thick, some thin, some cramped. / 同一工具条中，图标视觉重量必须一致，不能有的粗、有的细、有的挤。/ Dans la même barre d'outils, le poids visuel des icônes doit être cohérent ; pas de certaines épaisses, certaines fines, certaines serrées.

### 8.10 Layout Rhythm / 布局节奏 / Rythme de mise en page
#### Group Relationships / 组间关系 / Relations entre groupes
- **More whitespace between groups / 组与组之间留白更大 / Plus d'espace blanc entre les groupes**
- **Tighter whitespace within groups / 组内元素之间留白更紧 / Espace blanc plus serré au sein des groupes**

#### Visual Center / 视觉中心 / Centre visuel
- Each window is allowed only one visual protagonist: / 每个窗口只允许有一个视觉主角： / Chaque fenêtre n'est autorisée qu'un seul protagoniste visuel :
  - Settings page: the content group itself / 设置页：主角是内容组本身 / Page de paramètres : le groupe de contenu lui-même
  - Recording HUD: the timer / 录制中 HUD：主角是计时器 / HUD d'enregistrement : la minuterie
  - Recording configuration floating window: the main action button / 录制配置悬浮窗：主角是主操作按钮 / Fenêtre flottante de configuration d'enregistrement : le bouton d'action principal

### 8.11 Settings Page Standards / Settings 页规范 / Normes de la page de paramètres
- Focus on embodying "quiet, reliable, fatigue-free over long use." / 重点体现"安静、可靠、长时间使用不疲劳"。 / Met l'accent sur "calme, fiable, sans fatigue lors d'une utilisation prolongée".
- Do not make it a dashboard or marketing-style UI. / 不要做成仪表盘，也不要做成营销型 UI。 / Ne le faites pas comme un tableau de bord ou une interface marketing.
- Shortcuts only for high-frequency operations: / 快捷键只给高频操作： / Raccourcis uniquement pour les opérations à haute fréquence :
  - Screenshot / 截图 / Capture d'écran
  - Recording / 录制 / Enregistrement
  - Replay / 回录 / Replay
- Low-frequency actions remain in the settings page: / 低频动作继续留在设置页： / Les actions à faible fréquence restent dans la page de paramètres :
  - Save path / 保存路径 / Chemin de sauvegarde
  - Launch at startup / 开机自启动 / Lancer au démarrage
  - Audio preferences / 音频偏好 / Préférences audio
  - Quality / 画质 / Qualité

### 8.12 HUD Standards / HUD 规范 / Normes HUD
- The HUD must feel like a "tool floating on the desktop," not a standalone application window. / HUD 必须像"浮在桌面上的工具"，而不是独立应用窗口。/ Le HUD doit ressembler à un "outil flottant sur le bureau", pas une fenêtre d'application autonome.
- Background should be as transparent as possible; information layer should be as light as possible. / 背景尽量透明，信息层尽量轻。/ L'arrière-plan doit être aussi transparent que possible ; la couche d'informations doit être aussi légère que possible.
- In the main control bar: / 主控制条里： / Dans la barre de contrôle principale :
  - Current mode must be clearest / 当前模式必须最清楚 / Le mode actuel doit être le plus clair
  - Secondary tools must recede / 次级工具必须退后 / Les outils secondaires doivent se replier
  - Do not堆砌 icons / 不可堆砌图标 / Pas d'icônes entassées

### 8.13 Recording Active Floating Layer Standards / Recording Active 浮层规范 / Normes de la couche flottante d'enregistrement actif
- The visual center is always the time. / 视觉中心永远是时间。/ Le centre visuel est toujours l'heure.
- The volume bar only expresses feedback and does not抢 the main visual. / 音量条只表达反馈，不抢主视觉。/ La barre de volume n'exprime que le retour et ne vole pas le visuel principal.
- Pause and stop must be一眼可达, but should not appear bulky. / 暂停和停止必须一眼可达，但不能显得笨重。/ Pause et arrêt doivent être accessibles en un coup d'œil, mais ne doivent pas paraître encombrants.
- The more button must exist, but must be弱化. / 更多按钮必须存在，但必须弱化。/ Le bouton "plus" doit exister, mais doit être atténué.

### 8.14 Prohibited Items / 禁止事项 / Éléments interdits
- Prohibit large gray blocks堆砌 gray blocks. / 禁止大面积灰块叠灰块。/ Interdire les grands blocs gris empilés sur des blocs gris.
- Prohibit multiple shadow logics coexisting. / 禁止多个阴影逻辑并存。/ Interdire la coexistence de plusieurs logiques d'ombre.
- Prohibit too many blue blocks appearing on one page simultaneously. / 禁止一个页面同时出现太多蓝色块。/ Interdire l'apparition simultanée de trop de blocs bleus sur une page.
- Prohibit meaningless borders added for the sake of "refinement." / 禁止为了"精致"加入无意义描边。/ Interdire l'ajout de bordures dénuées de sens au nom du "raffinement".
- Prohibit making all functions shortcut-enabled. / 禁止把所有功能都快捷键化。/ Interdire de rendre toutes les fonctions accessibles par raccourci.
- Prohibit inconsistent icon styles. / 禁止图标风格混乱。/ Interdire les styles d'icônes incohérents.

### 8.15 Implementation Priority / 实施优先级 / Priorité de mise en œuvre
Subsequent UI adjustments should be done in the following order, not in reverse: / 后续所有 UI 调整，按下面顺序做，不要反过来： / Les ajustements UI ultérieurs doivent être effectués dans l'ordre suivant, pas dans l'ordre inverse :

1. First adjust layout and whitespace / 先调布局与留白 / D'abord ajuster la mise en page et l'espace blanc
2. Then adjust text hierarchy / 再调文字层级 / Puis ajuster la hiérarchie du texte
3. Then adjust color and materials / 再调颜色与材质 / Puis ajuster les couleurs et les matériaux
4. Finally adjust icons and shadows / 最后才调图标和阴影 / Enfin ajuster les icônes et les ombres

If the order is reversed, the interface easily becomes "partially refined, overall chaotic." / 如果顺序反了，界面很容易变成"局部精致，整体混乱"。/ Si l'ordre est inversé, l'interface devient facilement "partiellement raffinée, globalement chaotique".
