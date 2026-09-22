export const ROLES = {
  guard: { name: 'セラ', role: '防御', skill: '蒼の防壁', hp: 260, attack: 13, color: '#79cce0', description: '防壁で次の被害を半減。敵を崩し、与ダメージ +20%。' },
  support: { name: 'リネ', role: '支援', skill: '星の祈り', hp: 180, attack: 12, color: '#b7a0e8', description: '全員を20%回復。次のダメージ技を25%強化。' },
  attack: { name: 'アシュ', role: '攻撃', skill: '誓いの一閃', hp: 210, attack: 26, color: '#f0bf7c', description: '選択した部位へ、通常攻撃の3倍の一撃。' },
};
export const DEFAULT_ORDER = ['guard', 'support', 'attack'];
export const RELICS = {
  none: { name: '装備なし', description: '技に追加効果なし。' },
  relay: { name: '共鳴の灯', description: '技の後、同じ連携の以降の味方の攻撃技1回を30%強化。重複しない。' },
  ward: { name: '薄明の護符', description: '技の後、生存する全員に吸収30を付与。6秒間。重複せず更新。' },
  breaker: { name: '穿翼の欠片', description: '本人の攻撃技が左翼に与えるダメージを30%強化。本体には無効。' },
};
export const DEFAULT_RELICS = { guard: 'ward', support: 'relay', attack: 'breaker' };
function cleanRelics(value, fallback = DEFAULT_RELICS) {
  return Object.fromEntries(DEFAULT_ORDER.map(id => [id, typeof value?.[id] === 'string' && Object.hasOwn(RELICS, value[id]) ? value[id] : (fallback[id] || 'none')]));
}
const validOrder = order => Array.isArray(order) && order.length === 3 && new Set(order).size === 3 && order.every(k => typeof k === 'string' && Object.hasOwn(ROLES, k));
export function cleanSave(value) {
  const data = value && typeof value === 'object' ? value : {};
  const integer = (n, max) => Number.isSafeInteger(n) && n >= 0 ? Math.min(n, max) : 0;
  return { version: 2, materials: integer(data.materials, 999999), level: integer(data.level, 3), memory: data.memory === true, wins: integer(data.wins, 999999), order: validOrder(data.order) ? [...data.order] : [...DEFAULT_ORDER], relics: cleanRelics(data.relics) };
}
export function upgrade(save) {
  if (save.materials < 4 || save.level >= 3) return false;
  save.materials -= 4; save.level++; return true;
}
export function grantReward(save, battle) {
  if (battle.phase !== 'victory' || battle.rewardClaimed) return 0;
  battle.rewardClaimed = true;
  const amount = 3 + Number(battle.boss.wing <= 0);
  save.materials += amount; save.wins++; save.memory = true;
  return amount;
}

export class Battle {
  constructor({ order = DEFAULT_ORDER, level = 0, target = 'wing', auto = false, relics = {} } = {}) {
    if (!validOrder(order)) throw new Error('Invalid formation');
    const multiplier = 1 + Math.min(3, Math.max(0, level)) * .1;
    this.heroes = order.map((id, i) => ({ id, ...ROLES[id], hp: Math.round(ROLES[id].hp * multiplier), maxHp: Math.round(ROLES[id].hp * multiplier), attack: ROLES[id].attack * multiplier, nextAttack: i + 1, shieldUntil: 0, shield: false }));
    const equipment = cleanRelics(relics, {});
    this.heroes.forEach(h => { h.relic = equipment[h.id]; h.ward = 0; h.wardUntil = 0; });
    this.relayFrom = null;
    this.oath = 0; this.chainOath = 0; this.rescueUsed = false;
    this.stats = { entrusts: 0, chains: 0, maxChain: 0, released: 0 };
    this.boss = { hp: 1250, maxHp: 1250, wing: 280, maxWing: 280, vulnerableUntil: 0 };
    this.phase = 'running'; this.time = 0; this.paused = false; this.auto = auto;
    this.target = target === 'body' ? 'body' : 'wing'; this.candidate = 0; this.reserved = [];
    this.commandAt = 12; this.enemyAt = 5; this.enemyCursor = 0; this.bigAt = 24;
    this.warned = false; this.skipBig = false; this.boostUntil = 0;
    this.chain = []; this.chainIndex = 0; this.chainClock = 0; this.lastChain = 0;
    this.rewardClaimed = false; this.events = []; this.serial = 0;
    this.log('出撃。浮遊遺構「鐘を失くした方舟」に接近。', 'start');
  }
  log(text, kind = 'info', actor = null) {
    this.events.push({ id: ++this.serial, time: this.time, text, kind, actor });
    if (this.events.length > 80) this.events.shift();
  }
  nextAlive(after, excluded = []) {
    for (let n = 1; n <= this.heroes.length; n++) {
      const i = (after + n + this.heroes.length) % this.heroes.length;
      if (this.heroes[i].hp > 0 && !excluded.includes(i)) return i;
    }
    return -1;
  }
  normalize() {
    this.reserved = this.reserved.filter(i => this.heroes[i].hp > 0);
    if (this.candidate < 0 || this.heroes[this.candidate].hp <= 0 || this.reserved.includes(this.candidate)) {
      this.candidate = this.nextAlive(this.candidate, this.reserved);
    }
  }
  participants() {
    return [...this.reserved, ...(this.candidate >= 0 ? [this.candidate] : [])].filter(i => this.heroes[i].hp > 0);
  }
  canEntrust() {
    return this.phase === 'decision' && !this.paused && this.candidate >= 0 && this.reserved.length < 2 && this.nextAlive(this.candidate, this.participants()) >= 0;
  }
  entrust() {
    if (!this.canEntrust()) return false;
    this.reserved.push(this.candidate);
    this.oath = Math.min(3, this.oath + 1);
    this.stats.entrusts++;
    this.candidate = this.nextAlive(this.candidate, this.reserved);
    this.commandAt = this.time + 2;
    this.phase = 'running';
    this.log(`託す：2秒後に${this.heroes[this.candidate].name}へ。通常戦闘が進みます。`, 'entrust');
    return true;
  }
  selectTarget(target) {
    if (this.phase !== 'decision' || this.paused || !['body', 'wing'].includes(target) || (target === 'wing' && this.boss.wing <= 0)) return false;
    this.target = target; return true;
  }
  fire({ release = false } = {}) {
    if (this.phase !== 'decision' || this.paused) return false;
    this.normalize(); this.chain = this.participants();
    if (!this.chain.length) return false;
    this.chainOath = release ? this.oath : 0;
    this.stats.chains++; this.stats.maxChain = Math.max(this.stats.maxChain, this.chain.length);
    this.stats.released += this.chainOath;
    if (this.chainOath) {
      this.oath = 0;
      this.log(`誓いを${this.chainOath}解放：攻撃技・回復量＋${this.chainOath * 20}%、防壁＋${this.chainOath * 2}秒。`, 'oath');
    }
    this.lastChain = this.chain.length; this.chainIndex = 0; this.chainClock = 0;
    this.relayFrom = null;
    this.reserved = []; this.phase = 'chain'; this.applySkill(this.chain[0]);
    return true;
  }
  checkEnd() {
    if (['victory', 'defeat'].includes(this.phase)) return true;
    if (this.boss.hp <= 0) {
      this.boss.hp = 0; this.phase = 'victory'; this.log('討伐成功。失われた記憶が、翼に宿る。', 'victory'); return true;
    }
    if (this.heroes.every(h => h.hp <= 0)) {
      this.phase = 'defeat'; this.log('部隊撤退。編成と発動のタイミングを見直そう。', 'defeat'); return true;
    }
    return false;
  }
  hitBoss(base, skill = false, actor = null) {
    if (this.checkEnd()) return;
    const boost = skill && this.boostUntil > this.time;
    const relay = skill && this.phase === 'chain' && this.relayFrom !== null && this.relayFrom !== actor;
    const breaker = skill && this.heroes[actor]?.relic === 'breaker' && this.target === 'wing' && this.boss.wing > 0;
    const damage = Math.round(base * (this.boss.vulnerableUntil > this.time ? 1.2 : 1) * (boost ? 1.25 : 1) * (relay ? 1.3 : 1) * (breaker ? 1.3 : 1));
    if (relay) { this.relayFrom = null; this.log('共鳴の灯：味方の攻撃技を30%強化。', 'relic', actor); }
    if (breaker) this.log('穿翼の欠片：左翼への攻撃技を30%強化。', 'relic', actor);
    if (boost) this.boostUntil = 0;
    if (this.target === 'wing' && this.boss.wing > 0) {
      this.boss.wing = Math.max(0, this.boss.wing - damage);
      this.log(`翼に ${damage} ダメージ`, 'hit', actor);
      if (this.boss.wing === 0) {
        this.target = 'body'; this.skipBig = true;
        this.log('翼を破壊！ 次の大技を中断。以後の大技が30%弱体化。', 'break');
      }
    } else {
      this.boss.hp = Math.max(0, this.boss.hp - damage);
      this.log(`本体に ${damage} ダメージ`, 'hit', actor);
    }
    this.checkEnd();
  }
  hurt(index, damage) {
    const hero = this.heroes[index];
    if (hero.hp <= 0) return;
    if (hero.shield && hero.shieldUntil > this.time) { damage *= .5; hero.shield = false; }
    if (hero.ward > 0 && hero.wardUntil > this.time) {
      const absorbed = Math.min(hero.ward, Math.round(damage)); hero.ward -= absorbed; damage = Math.round(damage) - absorbed;
      this.log(`${hero.name}の護符が ${absorbed} ダメージを吸収。`, 'relic', index);
    }
    damage = Math.round(damage); hero.hp = Math.max(0, hero.hp - damage);
    this.log(`${hero.name}に ${damage} ダメージ${hero.hp <= 0 ? '／戦闘不能' : ''}`, 'hurt', index);
    const support = this.heroes.find(h => h.id === 'support');
    if (damage > 0 && hero.hp > 0 && hero.hp <= hero.maxHp * .25 && support?.hp > 0 && !this.rescueUsed) {
      this.rescueUsed = true;
      const restored = Math.min(hero.maxHp - hero.hp, Math.round(hero.maxHp * .3));
      hero.hp += restored;
      this.log(`リネの緊急救援：${hero.name}を${restored}回復（戦闘中1回）。`, 'rescue', index);
    }
  }
  applySkill(index) {
    const hero = this.heroes[index];
    if (hero.hp <= 0 || this.checkEnd()) return;
    this.log(`${this.chainIndex + 1} CHAIN ─ ${hero.name}「${hero.skill}」`, 'skill', index);
    if (hero.id === 'guard') {
      for (const h of this.heroes) if (h.hp > 0) { h.shield = true; h.shieldUntil = this.time + 6 + this.chainOath * 2; }
      this.boss.vulnerableUntil = this.time + 6;
    } else if (hero.id === 'support') {
      for (const h of this.heroes) if (h.hp > 0) h.hp = Math.min(h.maxHp, h.hp + Math.round(h.maxHp * .2 * (1 + this.chainOath * .2)));
      this.boostUntil = this.time + 6;
    } else this.hitBoss(hero.attack * 3 * (1 + this.chainOath * .2), true, index);
    if (this.checkEnd()) return;
    if (hero.relic === 'relay' && this.phase === 'chain') {
      this.relayFrom = index;
      this.log(`${hero.name}の共鳴の灯：以降の味方の攻撃技を待つ（この連携限り）。`, 'relic', index);
    } else if (hero.relic === 'ward') {
      for (const h of this.heroes) if (h.hp > 0) { h.ward = 30; h.wardUntil = this.time + 6; }
      this.log(`${hero.name}の薄明の護符：全員に吸収30／6秒。`, 'relic', index);
    }
  }
  step(delta) {
    if (this.paused || !Number.isFinite(delta) || delta <= 0 || ['victory', 'defeat'].includes(this.phase)) return;
    // Substeps keep event ordering independent of rendering frame rate.
    let remaining = Math.min(delta, .25);
    while (remaining > 1e-8) {
      const dt = Math.min(remaining, .05); remaining -= dt;
      if (this.phase === 'decision') { if (this.auto) this.fire(); else return; }
      if (this.phase === 'chain') {
        this.chainClock += dt;
        if (this.chainClock + 1e-8 >= .6) {
          this.chainClock -= .6; this.chainIndex++;
          if (this.chainIndex < this.chain.length) this.applySkill(this.chain[this.chainIndex]);
          else {
            this.chainOath = 0;
            this.relayFrom = null;
            this.candidate = this.nextAlive(this.chain.at(-1));
            this.phase = 'running'; this.commandAt = this.time + 12;
          }
        }
        continue;
      }
      if (this.phase !== 'running') return;
      this.time = Math.round((this.time + dt) * 1e8) / 1e8;
      for (let i = 0; i < this.heroes.length; i++) {
        const hero = this.heroes[i];
        if (hero.hp > 0 && this.time + 1e-7 >= hero.nextAttack) {
          hero.nextAttack += 3; this.hitBoss(hero.attack * (this.oath >= 2 ? 1.1 : 1), false, i);
          if (this.checkEnd()) return;
        }
      }
      if (this.time + 1e-7 >= this.enemyAt) {
        const target = this.nextAlive(this.enemyCursor - 1);
        this.enemyAt += 5; this.enemyCursor = (target + 1) % this.heroes.length;
        if (target >= 0) this.hurt(target, 28);
        if (this.checkEnd()) return;
      }
      if (!this.warned && this.time + 1e-7 >= this.bigAt - 6) {
        this.warned = true;
        this.log(this.skipBig ? '翼破壊により、次の全体攻撃は不発。' : '全体攻撃「落星」まで6秒。', 'warning');
      }
      if (this.time + 1e-7 >= this.bigAt) {
        if (this.skipBig) { this.skipBig = false; this.log('落星を中断した。', 'break'); }
        else {
          this.log('落星 ─ 全体攻撃！', 'blast');
          for (let i = 0; i < this.heroes.length; i++) this.hurt(i, 65 * (this.boss.wing <= 0 ? .7 : 1));
        }
        this.bigAt += 24; this.warned = false;
        if (this.checkEnd()) return;
      }
      if (this.time + 1e-7 >= this.commandAt) {
        this.normalize(); this.phase = 'decision';
        this.log('指示待ち。時間は停止しています。', 'decision');
      }
    }
  }
}
