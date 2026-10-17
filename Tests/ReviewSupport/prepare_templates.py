"""从已核对的章节测试档生成审核入口；运行时不依赖此工具。"""
import copy
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
destination = root / 'Assets/StreamingAssets/ReviewSaves'
configs = [
    (2, 2, 'Chapter 2 opening', 2, 'StealthPlatform', 'EXC2-1', None),
    (3, 3, 'Chapter 3 opening', 3, 'Settlement', 'EXC3-1', None),
    (4, 4, 'Chapter 4 opening', 4, 'Confrontation', 'EXC4-1', None),
    (5, 5, 'Death ending', 1, 'YamaClass', 'EXC1-3-B', ['EXC1-2']),
    (6, 6, 'Injury / bleeding finale', 4, 'Confrontation', 'EXC4-F', None),
    (7, 7, 'Attack / injury', 2, 'StealthPlatform', 'EXC2-8-B', None),
    (8, 8, 'Suicide reference', 1, 'YamaClass', 'EXC1-3', ['EXC1-2']),
]
entries = []
for key, slot, title, chapter, map_name, script, nodes in configs:
    seed_chapter = max(2, chapter)
    profile = copy.deepcopy(json.loads((root / f'Artifacts/ChapterStartSaves/PlayerProfiles_{seed_chapter}.json').read_text(encoding='utf-8-sig')))
    if nodes is not None:
        profile['finishNodeDic'] = dict.fromkeys(nodes, 1)
    if key == 6:
        profile['finishNodeDic'].update(dict.fromkeys(['EXC4-1', 'EXC4-2', 'EXC4-3'], 1))
    if key == 7:
        profile['finishNodeDic'].update(dict.fromkeys(['EXC2-4', 'EXC2-5', 'EXC2-6', 'EXC2-7'], 1))
    profile.update(currentMap=map_name, currentScene='AVG', curSmallMapIndex=0,
                   chapterNumber=chapter, chapterTitle=['', '第一章', '第二章', '第三章', '第四章'][chapter],
                   currentStoryId=title, currentScriptFile=script, resumeStoryScript=script,
                   isStoryInProgress=True, isNewGame=False)
    filename = script + '.json'
    (destination / filename).write_text(json.dumps(profile, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    entries.append(dict(shortcutNumber=key, slot=slot, title=title, file=filename,
                        script=script, map=map_name, chapter=chapter))
(destination / 'review.json').write_text(json.dumps(dict(entries=entries), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(f'Prepared {len(entries)} review templates.')
