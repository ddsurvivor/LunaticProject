"""检查实际审核模板及当前场景的章节入口和前置任务关系。"""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / 'Assets/StreamingAssets/ReviewSaves'
scene = (root / 'Assets/Scenes/AVG.unity').read_text(encoding='utf-8-sig')
entries = json.loads((source / 'review.json').read_text(encoding='utf-8'))['entries']
assert len(entries) == 7
assert len({e['shortcutNumber'] for e in entries}) == len({e['slot'] for e in entries}) == 7
required = {
    2: ['EXC1-2', 'EXC1-3', 'EXC1-4'],
    3: ['EXC1-2', 'EXC1-3', 'EXC1-4', 'EXC2-4', 'EXC2-5', 'EXC2-6', 'EXC2-7', 'EXC2-8'],
    4: ['EXC1-2', 'EXC1-3', 'EXC1-4', 'EXC2-4', 'EXC2-5', 'EXC2-6', 'EXC2-7', 'EXC2-8', 'EXC3-2', 'EXC3-3', 'EXC3-4', 'EXC3-5'],
}
for entry in entries:
    p = json.loads((source / entry['file']).read_text(encoding='utf-8'))
    assert p['currentMap'] == entry['map'] and f"m_Name: {entry['map']}\n" in scene
    assert (root / f"Assets/Resources/Story/{entry['script']}.bytes").exists()
    assert p['currentScene'] == 'AVG' and p['chapterNumber'] == entry['chapter']
    assert p['isStoryInProgress'] and not p['isNewGame']
    assert p['resumeStoryScript'] == p['currentScriptFile'] == entry['script']
    assert p['finishNodeDic'].get(entry['script'], 0) == 0
    for node in required.get(entry['shortcutNumber'], []):
        assert p['finishNodeDic'].get(node) == 1, (entry, node)
    assert len(p['player']) == 20 and all(p['player'][i]['HP'] > 0 for i in range(3))
build_settings = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text(encoding='utf-8-sig')
assert re.search(r'enabled: 1\s+path: Assets/Scenes/AVG\.unity', build_settings)
assert not list((root / 'Assets/StreamingAssets/Datas').glob('PlayerProfiles_*.json'))
print('PASS: 7 review entries, chapter progress, maps, story assets and enabled AVG scene.')
