"""生成战斗通用白色 UI 素材、Unity 导入配置与设计预览（依赖 Pillow）。"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from collections import Counter
import json, re, uuid, math
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/A美术/BattleCommonUI'
DOC = ROOT / 'Docs/UI/BattleCommonUI'
OUT.mkdir(parents=True, exist_ok=True)
DOC.mkdir(parents=True, exist_ok=True)
S = 4
assets = []
fontpath = 'C:/Windows/Fonts/msyh.ttc'
def font(n): return ImageFont.truetype(fontpath,n)
def shape(w,h):
    im=Image.new('RGBA',(w*S,h*S));return im,ImageDraw.Draw(im)
def poly(d,pts,fill=None,line=None,width=2):
    pts=[(int(x*S),int(y*S)) for x,y in pts]
    if fill:d.polygon(pts,fill=fill)
    if line:d.line(pts+[pts[0]],fill=line,width=width*S,joint='curve')
def line(d,pts,width=2): d.line([(int(x*S),int(y*S)) for x,y in pts],fill='white',width=width*S,joint='curve')
def save(name,im,border=(0,0,0,0),usage='Simple'):
    im=im.resize((im.width//S,im.height//S),Image.Resampling.LANCZOS)
    # RGB 恒为白色，仅 alpha 保存轮廓，保证任意染色不受灰边影响。
    im=Image.merge('RGBA',(*[Image.new('L',im.size,255) for _ in range(3)],im.getchannel('A')))
    im.save(OUT/(name+'.png'))
    assets.append(dict(name=name,size=list(im.size),border=list(border),imageType=usage))
    meta=ROOT/'Assets/A美术/AVG新版UI/对话框/外框.png.meta'
    text=meta.read_text(encoding='utf-8')
    target=OUT/(name+'.png.meta')
    guid=re.search(r'guid: (\w+)',target.read_text()).group(1) if target.exists() else uuid.uuid4().hex
    text=re.sub(r'guid: \w+', 'guid: '+guid,text,count=1)
    text=re.sub(r'spriteBorder: .*', 'spriteBorder: {x: %d, y: %d, z: %d, w: %d}'%border,text)
    text=text.replace('textureCompression: 1','textureCompression: 0')
    target.write_text(text,encoding='utf-8')
# 角部保留 20px，九宫格只拉伸中间直线区。
for name,w,h,cut in [('Panel',128,96,16),('Header',128,48,12),('Button',128,64,12),('Portrait',96,96,12),('Status',128,40,8),('BuffSlot',48,48,6),('MenuRow',160,48,8)]:
    pts=[(2,2),(w-cut-2,2),(w-2,cut+2),(w-2,h-2),(cut+2,h-2),(2,h-cut-2)]
    for layer in ['Fill','Outline']:
        im,d=shape(w,h);poly(d,pts,fill='white' if layer=='Fill' else None,line='white' if layer=='Outline' else None)
        save(name+layer,im,(20,20,20,20) if h>=48 else (12,12,12,12),'Sliced')
for name in ['BarFill','BarOutline']:
    im,d=shape(64,16);poly(d,[(2,2),(62,2),(62,14),(2,14)],fill='white' if name=='BarFill' else None,line='white' if name=='BarOutline' else None)
    save(name,im,(4,4,4,4),'Sliced' if name=='BarOutline' else 'Filled')
for name in ['APFilled','APOutline','Selector','Chevron','Divider','AimDash']:
    im,d=shape(32,32)
    if name.startswith('AP'):poly(d,[(16,3),(29,16),(16,29),(3,16)],fill='white' if name=='APFilled' else None,line='white' if name=='APOutline' else None)
    elif name=='Selector':poly(d,[(8,5),(24,16),(8,27)],fill='white')
    elif name=='Chevron':line(d,[(8,5),(22,16),(8,27)],3)
    elif name=='Divider':line(d,[(0,16),(32,16)],1)
    else:line(d,[(8,16),(24,16)],2)
    save(name,im,usage='Tiled' if name in ['Divider','AimDash'] else 'Simple')
for name in ['Move','Attack','Skill','Item','Guard','Wait','BuffUp','DebuffDown']:
    im,d=shape(64,64)
    if name=='Move':
        line(d,[(12,48),(46,14)],4);line(d,[(26,14),(46,14),(46,34)],4)
    elif name=='Attack':
        poly(d,[(18,42),(43,12),(53,10),(52,20),(26,48)],line='white',width=3);line(d,[(12,35),(33,54)],3);line(d,[(19,46),(10,55)],4)
    elif name=='Skill':
        poly(d,[(35,6),(15,35),(29,35),(25,58),(50,24),(35,24)],line='white',width=3)
    elif name=='Item':
        poly(d,[(23,9),(41,9),(41,19),(48,29),(48,52),(16,52),(16,29),(23,19)],line='white',width=3);line(d,[(23,37),(41,37)],3);line(d,[(32,28),(32,46)],3)
    elif name=='Guard':
        poly(d,[(10,12),(32,7),(54,12),(50,38),(32,56),(14,38)],line='white',width=3);line(d,[(32,16),(32,44)],3)
    elif name=='Wait':
        d.ellipse((9*S,9*S,55*S,55*S),outline='white',width=3*S);line(d,[(32,17),(32,33),(43,39)],3)
    else:
        pts=[(15,36),(32,18),(49,36)] if name=='BuffUp' else [(15,26),(32,44),(49,26)]
        line(d,pts,4);line(d,[(32,20),(32,48)] if name=='BuffUp' else [(32,16),(32,42)],4)
    save('Icon'+name,im)
for name in ['RangeCellFill','RangeCellOutline','RangeCellHatch','RangeCircleFill','RangeCircleOutline','RangeConeFill','RangeLine','TargetReticle','SelectedRing']:
    im,d=shape(128,128)
    if 'Cell' in name:
        pts=[(4,4),(124,4),(124,124),(4,124)]
        if 'Fill' in name:poly(d,pts,fill='white')
        elif 'Outline' in name:poly(d,pts,line='white',width=3)
        else:
            for k in range(-120,130,18):line(d,[(max(4,k),max(4,4-k)),(min(124,k+120),min(124,124-k))],2)
    elif name=='TargetReticle':
        for x,y,sx,sy in [(8,8,1,1),(120,8,-1,1),(8,120,1,-1),(120,120,-1,-1)]:line(d,[(x,y+sy*24),(x,y),(x+sx*24,y)],4)
        line(d,[(52,64),(76,64)],2);line(d,[(64,52),(64,76)],2)
    elif name=='RangeConeFill':
        d.pieslice((4*S,4*S,124*S,124*S),225,315,fill='white')
    elif name=='RangeLine':poly(d,[(56,4),(72,4),(72,124),(56,124)],fill='white')
    else:d.ellipse((5*S,5*S,123*S,123*S),fill='white' if name=='RangeCircleFill' else None,outline=None if name=='RangeCircleFill' else 'white',width=3*S)
    save(name,im)
(OUT/'manifest.json').write_text(json.dumps(assets,ensure_ascii=False,indent=2),encoding='utf-8')
# 逐张记录不透明像素的精确主色，避免把抗锯齿及半透明混色当成主题色。
records=[]
for group in ['对话框','图像设置','存档']:
    for p in sorted((ROOT/'Assets/A美术/AVG新版UI'/group).glob('*.png')):
        im=Image.open(p).convert('RGBA');c=Counter((r,g,b) for r,g,b,a in im.getdata() if a>=250)
        records.append(dict(source=p.relative_to(ROOT).as_posix(),colors=[dict(hex='#%02X%02X%02X'%rgb,pixels=n) for rgb,n in c.most_common(8)]))
(DOC/'AVGColorExtraction.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
palette=[('Background','#11181E'),('Surface','#243042'),('SurfaceInset','#202C3D'),('Border','#A59177'),('Accent','#E7A533'),('Text','#EEE6CF'),('TextHeading','#E5E0CF'),('TextMuted','#A3ABAF'),('Divider','#687A85'),('Disabled','#576774'),('Ally *','#4AC3D5'),('Enemy *','#EA6963'),('Positive / Health *','#7DC59E')]
sheet=Image.new('RGB',(1280,1120),'#11181E');d=ImageDraw.Draw(sheet)
d.text((32,24),'BATTLE / 通用白色素材 · 运行时染色',font=font(28),fill='#EEE6CF')
for i,a in enumerate(assets):
    x=32+i%7*178;y=88+i//7*135
    im=Image.open(OUT/(a['name']+'.png'));im.thumbnail((140,80));sheet.paste(im,(x,y),im)
    d.text((x,y+85),a['name'],font=font(12),fill='#EEE6CF');d.text((x,y+103),str(a['size'])+' / '+a['imageType'],font=font(11),fill='#A3ABAF')
sheet.save(DOC/'AssetOverview.png')
p=Image.new('RGB',(1000,730),'#11181E');d=ImageDraw.Draw(p)
d.text((28,18),'AVG 采样色板 / * 战斗扩展色',font=font(26),fill='#EEE6CF')
for i,(name,col) in enumerate(palette):
    x=28+i%2*490;y=80+i//2*90;d.rectangle((x,y,x+80,y+62),fill=col,outline='#687A85');d.text((x+100,y+4),name,font=font(19),fill='#EEE6CF');d.text((x+100,y+32),col,font=font(18),fill='#A3ABAF')
p.save(DOC/'Palette.png')
print('Generated',len(assets),'sprites;',len(records),'source images sampled')

# 使用交付素材合成精确布局，供程序按色板与锚点复现。
canvas=Image.new('RGBA',(1920,1080),'#0D151C');dd=ImageDraw.Draw(canvas)
def txt(x,y,s,n=22,c='#EEE6CF'): ImageDraw.Draw(canvas).text((x,y),s,font=font(n),fill=c)
def sprite(name,box,color,angle=0):
    x,y,w,h=box;im=Image.open(OUT/(name+'.png')).convert('RGBA')
    a=next(a for a in assets if a['name']==name);l,b,r,t=a['border']
    if any(a['border']) and w>l+r and h>t+b:
        # 与 Unity Sliced 一样固定角部，只伸缩边和中心。
        sw,sh=im.size;patch=Image.new('RGBA',(w,h));xs=[0,l,sw-r,sw];ys=[0,t,sh-b,sh];xd=[0,l,w-r,w];yd=[0,t,h-b,h]
        for j in range(3):
            for i in range(3):
                tile=im.crop((xs[i],ys[j],xs[i+1],ys[j+1])).resize((xd[i+1]-xd[i],yd[j+1]-yd[j]),Image.Resampling.LANCZOS);patch.paste(tile,(xd[i],yd[j]))
        im=patch
    else: im=im.resize((w,h),Image.Resampling.LANCZOS)
    from PIL import ImageColor
    rgba=ImageColor.getcolor(color,'RGBA');im.putalpha(im.getchannel('A').point(lambda a:a*rgba[3]//255));tint=Image.new('RGBA',im.size,rgba);tint.putalpha(im.getchannel('A'));canvas.alpha_composite(tint.rotate(angle),(x,y))
def panel(x,y,w,h):
    sprite('PanelFill',(x,y,w,h),'#11181EEF');sprite('PanelOutline',(x,y,w,h),'#A59177')
def ap(x,y,used,total=3):
    for j in range(total):sprite('APFilled' if j<used else 'APOutline',(x+j*25,y,19,19),'#E7A533')
def bar(x,y,w,value):
    sprite('BarFill',(x,y,w,14),'#243042');sprite('BarFill',(x,y,int(w*value),14),'#7DC59E')
# 斜视棋盘示意：范围遮罩与棋盘共享投影，避免屏幕正方形贴图误导实现。
def cell(i,j):
    x=960+(i-j)*76;y=260+(i+j)*39
    return [(x,y),(x+73,y+37),(x,y+74),(x-73,y+37)]
for i in range(7):
    for j in range(7):
        pts=cell(i,j);ImageDraw.Draw(canvas).polygon(pts,fill='#15222D',outline='#34434E')
        if 1<=i<=3 and 2<=j<=4:ImageDraw.Draw(canvas).polygon(pts,fill='#20414C',outline='#4AC3D5')
        if 3<=i<=5 and 0<=j<=2:ImageDraw.Draw(canvas).polygon(pts,fill='#493D23',outline='#E7A533')
for i,j,label,team,hp in [(2,3,'我方 01','#4AC3D5',.7),(4,1,'敌方 01','#EA6963',.6),(6,3,'敌方 02','#EA6963',.9)]:
    x,y=cell(i,j)[0];ImageDraw.Draw(canvas).ellipse((x-16,y+12,x+16,y+50),fill=team);sprite('SelectedRing',(x-35,y+4,70,52),team)
    panel(x-86,y-88,172,78);txt(x-72,y-82,label,16,team);bar(x-70,y-52,140,hp);ap(x-70,y-28,2)
    sprite('IconBuffUp',(x+19,y-29,20,20),'#7DC59E');sprite('IconGuard',(x+44,y-29,20,20),'#EEE6CF')
x,y=cell(4,1)[0];sprite('TargetReticle',(x-52,y-5,104,80),'#EA6963')
for k in range(10):
    xx=884+k*29;yy=513-k*9;sprite('AimDash',(xx,yy,24,10),'#E7A533')
panel(510,30,900,124);txt(536,46,'行动序列   /   100 ← 0',20,'#E7A533')
ImageDraw.Draw(canvas).line((560,116,1350,116),fill='#A59177',width=2)
for i in range(7):
    x=574+i*119;sprite('PortraitOutline',(x,78,40,40),'#4AC3D5' if i%2==0 else '#EA6963');txt(x+12,84,str(i+1),18)
# 与 BattleScene.prefab 的左下信息面板保持一致；头像窗口高于装饰框。
sprite('PanelFill',(14,730,520,336),'#0B1015AD')
panel(24,860,500,196)
sprite('PanelFill',(220,878,284,160),'#141D25')
sprite('PortraitOutline',(30,826,184,186),'#A59177')
portrait=Image.open(ROOT/'Assets/Resources/CG/PC01C1.png').convert('RGBA').resize((450,450),Image.Resampling.LANCZOS)
portrait_window=Image.new('RGBA',(184,270));portrait_window.paste(portrait,(-143,0),portrait)
canvas.alpha_composite(portrait_window,(30,742))
sprite('HeaderFill',(30,1012,184,36),'#243042');txt(96,1016,'邱悟',22)
# 标签共用左边缘，当前值右对齐；最大值紧随其后，保持同一基线。
for bottom,label,current,maximum,color in [(144,'行动力','6','/6','#E7A533'),(106,'生命','64','/64','#EEE6CF'),(64,'能量','13','/13','#EEE6CF'),(22,'弹药','6','','#EEE6CF')]:
    cy=1080-24-bottom-15
    draw=ImageDraw.Draw(canvas)
    draw.text((242,cy),label,font=font(18),fill='#A3ABAF',anchor='lm')
    if label=='行动力':
        for i in range(6):sprite('APFilled' if i<4 else 'APOutline',(int(318+27*(i+.5)-11),int(cy-11),22,22),'#4AC3D5' if i<4 else '#A59177')
    else:
        draw.text((408,cy),current,font=font(22),fill=color,anchor='rm')
        draw.text((412,cy),maximum,font=font(20),fill='#A3ABAF',anchor='lm')
for bottom,height,color in [(94,12,'#7DC59E'),(55,8,'#4AC3D5'),(14,8,'#E7A533')]:
    sprite('BarFill',(242,1080-24-bottom-height,238,height),color)
# 按棋子位置展开一级列表，技能二级菜单沿所选行展开。
menu_x,menu_y=968,490
menu_width,row_height,row_step=230,44,50
actions=[('移动','1 AP'),('攻击',None),('技能',None),('道具',None),('警戒指令',None),('待机','全部 AP')]
def menu(selected=False):
    for i,(label,cost) in enumerate(actions):
        y=menu_y+i*row_step;col='#E7A533' if selected and i==2 else '#A59177'
        sprite('MenuRowFill',(menu_x,y,menu_width,row_height),'#243042F5');sprite('MenuRowOutline',(menu_x,y,menu_width,row_height),col)
        txt(menu_x+18,y+7,label,20,'#EEE6CF' if not(selected and i==2) else '#E7A533')
        if cost is None:sprite('Selector',(menu_x+198,y+13,18,18),col,-90 if selected and i==2 else 0)
        else:txt(menu_x+148,y+10,cost,15,col)
        if selected and i==2:sprite('Selector',(menu_x+3,y+13,14,14),'#E7A533')
menu(False)
txt(1410,938,'棋子身边展开两级菜单',20);txt(1410,978,'技能详情固定左上角',20,'#A3ABAF')
canvas.convert('RGB').save(DOC/'BattleActions.png')
menu(True)
for i,(name,cost) in enumerate([('脉冲射击','2 AP'),('穿透射击','2 AP'),('战术支援','3 AP')]):
    x=menu_x+menu_width+8;y=menu_y+2*row_step+row_height//2-3*row_step//2+i*row_step;col='#E7A533' if i==0 else '#A59177'
    sprite('MenuRowFill',(x,y,menu_width,row_height),'#243042F5');sprite('MenuRowOutline',(x,y,menu_width,row_height),col);txt(x+18,y+7,name,20,col);txt(x+154,y+10,cost,15,col)
# 悬停技能时详情固定左上角，离开时隐藏；点击后关闭两级菜单进入瞄准。
panel(32,176,456,286);txt(56,194,'技能详情 / 脉冲射击',25,'#E7A533');txt(56,242,'消耗 2 AP  ·  射程 4 格',21);txt(56,282,'范围与伤害读取实际技能配置',21);txt(56,328,'对目标范围内敌人造成伤害。',20);txt(56,398,'技能悬停详情 · 左上固定',18,'#A3ABAF')
txt(968,450,'一级列表                 技能二级菜单',18,'#A3ABAF')
canvas.convert('RGB').save(DOC/'BattleSkills.png')
canvas.convert('RGB').save(DOC/'BattleLayout.png')

