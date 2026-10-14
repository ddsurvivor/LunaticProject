import copy
import json
import os
from datetime import datetime, timezone, timedelta
from pathlib import Path

root = Path(__file__).resolve().parent
source = Path(os.environ['USERPROFILE']) / 'AppData/LocalLow/PristineArchiveStudio/EI_ Iris2/Datas/PlayerProfiles_0.json'
base = json.loads(source.read_text(encoding='utf-8-sig'))
completed = ['EXC1-2', 'EXC1-3', 'EXC1-4']
configs = [
    (2, 2, '第二章', 'StealthPlatform', 'EXC2-1', list(completed)),
    (3, 3, '第三章', 'Settlement', 'EXC3-1', completed + ['EXC2-4', 'EXC2-5', 'EXC2-6', 'EXC2-7', 'EXC2-8']),
    (4, 4, '第四章', 'Confrontation', 'EXC4-1', completed + ['EXC2-4', 'EXC2-5', 'EXC2-6', 'EXC2-7', 'EXC2-8', 'EXC3-2', 'EXC3-3', 'EXC3-4', 'EXC3-5']),
]
for slot, chapter, title, map_name, story, nodes in configs:
    profile = copy.deepcopy(base)
    profile.update(finishNodeDic=dict.fromkeys(nodes, 1), currentMap=map_name,
                   currentScene='AVG', curSmallMapIndex=0, isNewGame=False,
                   chapterNumber=chapter, chapterTitle=title,
                   currentStoryId=title + '开头', currentScriptFile=story,
                   isStoryInProgress=True, resumeStoryScript=story,
                   lastSaveTime=datetime.now(timezone(timedelta(hours=8))).isoformat())
    output = root / f'PlayerProfiles_{slot}.json'
    output.write_text(json.dumps(profile, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    reread = json.loads(output.read_text(encoding='utf-8'))
    assert reread == profile
    assert story not in profile['finishNodeDic']
    assert len(profile['player']) == 20 and all(profile['player'][i]['HP'] > 0 for i in range(3))
    print(f'{output.name}: {title}, {map_name}, {story}, {len(nodes)} completed nodes')
