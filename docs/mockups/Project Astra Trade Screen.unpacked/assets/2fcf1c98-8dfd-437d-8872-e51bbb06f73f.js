// Variant 2 — TEMPLE BRASS
// Layout: horizontal brass plaque, portraits as oval cameos inside ogee arches,
// inventories as engraved brass columns on parchment panels between them.

const BRASS = {
  parchment: '#e8d9a8',
  parchmentDeep: '#c9b57d',
  parchmentShadow: '#8f7d4a',
  ink: '#2b1810',
  inkDeep: '#140a05',
  brass: '#b88835',
  brassLite: '#e4be6a',
  brassDeep: '#7a5a20',
  brassGlow: '#fde49a',
  vermillion: '#9e2a1c',
  saffron: '#d97a1f',
  jade: '#2f6b4d',
};

function BrassRow({ sigil, name, qty, state = 'default', align = 'left' }) {
  const palette = {
    default: { bg: 'rgba(184,136,53,0.0)', border: 'rgba(43,24,16,0.15)', text: BRASS.ink, shadow: 'none', sigilColor: BRASS.brassDeep },
    hover:   { bg: 'rgba(184,136,53,0.12)', border: 'rgba(43,24,16,0.3)', text: BRASS.ink, shadow: 'none', sigilColor: BRASS.brass },
    pressed: { bg: 'rgba(43,24,16,0.25)', border: BRASS.ink, text: BRASS.parchment, shadow: 'inset 0 2px 4px rgba(0,0,0,0.4)', sigilColor: BRASS.brassLite },
    focused: { bg: 'rgba(228,190,106,0.25)', border: BRASS.ink, text: BRASS.ink, shadow: `0 0 0 2px ${BRASS.brassLite}, inset 0 0 8px rgba(253,228,154,0.4)`, sigilColor: BRASS.brassDeep },
    selected:{ bg: 'rgba(158,42,28,0.85)', border: BRASS.ink, text: BRASS.parchment, shadow: `inset 0 0 0 2px ${BRASS.brassLite}`, sigilColor: BRASS.brassGlow },
    disabled:{ bg: 'rgba(143,125,74,0.1)', border: 'transparent', text: 'rgba(43,24,16,0.35)', shadow: 'none', sigilColor: 'rgba(122,90,32,0.5)', dim: true },
  };
  const s = palette[state];

  return (
    <div style={{
      display: 'grid',
      gridTemplateColumns: align === 'left' ? '40px 1fr 64px' : '64px 1fr 40px',
      alignItems: 'center', gap: 12,
      height: 54, padding: '0 16px',
      background: s.bg,
      borderTop: `1px solid ${s.border}`,
      borderBottom: `1px solid ${s.border}`,
      boxShadow: s.shadow,
      position: 'relative',
    }}>
      {align === 'left' ? (
        <>
          <ItemSigil type={sigil} color={s.sigilColor} dim={s.dim} size={30} />
          <div style={{
            fontFamily: 'Cormorant Garamond, serif', fontSize: 22, fontWeight: 500,
            color: s.text, letterSpacing: 0.2,
            textDecoration: s.dim ? 'line-through' : 'none',
          }}>{name}</div>
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 18, fontWeight: 600,
            color: s.text, textAlign: 'right', fontVariantNumeric: 'tabular-nums',
          }}>{qty}</div>
        </>
      ) : (
        <>
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 18, fontWeight: 600,
            color: s.text, textAlign: 'left', fontVariantNumeric: 'tabular-nums',
          }}>{qty}</div>
          <div style={{
            fontFamily: 'Cormorant Garamond, serif', fontSize: 22, fontWeight: 500,
            color: s.text, letterSpacing: 0.2, textAlign: 'right',
            textDecoration: s.dim ? 'line-through' : 'none',
          }}>{name}</div>
          <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <ItemSigil type={sigil} color={s.sigilColor} dim={s.dim} size={30} />
          </div>
        </>
      )}
      {state === 'selected' && (
        <div style={{
          position: 'absolute',
          [align === 'left' ? 'right' : 'left']: -6, top: '50%',
          transform: 'translateY(-50%)',
          width: 0, height: 0,
          borderTop: '10px solid transparent',
          borderBottom: '10px solid transparent',
          [align === 'left' ? 'borderLeft' : 'borderRight']: `10px solid ${BRASS.vermillion}`,
        }} />
      )}
    </div>
  );
}

function BrassCameo({ unit, side }) {
  return (
    <div style={{ position: 'relative', width: 360, height: 480 }}>
      {/* ogee arch frame */}
      <svg width="360" height="480" viewBox="0 0 360 480" style={{ position: 'absolute', inset: 0 }}>
        <defs>
          <linearGradient id={`brass-grad-${side}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={BRASS.brassLite} />
            <stop offset="50%" stopColor={BRASS.brass} />
            <stop offset="100%" stopColor={BRASS.brassDeep} />
          </linearGradient>
        </defs>
        {/* outer arch silhouette */}
        <path d="M 10 470 L 10 200 Q 10 90 90 70 Q 140 60 180 10 Q 220 60 270 70 Q 350 90 350 200 L 350 470 Z"
          fill={`url(#brass-grad-${side})`} stroke={BRASS.inkDeep} strokeWidth="2" />
        {/* inner arch cutout */}
        <path d="M 28 450 L 28 205 Q 28 105 100 88 Q 146 78 180 36 Q 214 78 260 88 Q 332 105 332 205 L 332 450 Z"
          fill={BRASS.inkDeep} stroke={BRASS.brassDeep} strokeWidth="1" />
        {/* decorative dots on arch */}
        {[...Array(10)].map((_, i) => {
          const t = i / 9;
          const angle = Math.PI * (0.15 + t * 0.7);
          const cx = 180 + Math.cos(angle + Math.PI) * 150;
          const cy = 200 - Math.sin(angle) * 140;
          return <circle key={i} cx={cx} cy={cy} r="2.5" fill={BRASS.inkDeep} />;
        })}
      </svg>
      {/* portrait inside */}
      <div style={{
        position: 'absolute',
        top: 40, left: 32, right: 32, bottom: 30,
        clipPath: 'path("M 0 410 L 0 170 Q 0 75 64 58 Q 106 48 148 6 Q 190 48 232 58 Q 296 75 296 170 L 296 410 Z")',
        overflow: 'hidden',
      }}>
        <PortraitPlaceholder width={296} height={410} name={unit.name} epithet={unit.epithet} tone="indigo" />
      </div>
      {/* nameplate */}
      <div style={{
        position: 'absolute', bottom: -20, left: 40, right: 40,
        background: `linear-gradient(180deg, ${BRASS.brassLite}, ${BRASS.brass} 50%, ${BRASS.brassDeep})`,
        border: `1.5px solid ${BRASS.inkDeep}`,
        padding: '10px 16px',
        textAlign: 'center',
        boxShadow: '0 4px 8px rgba(0,0,0,0.4)',
      }}>
        <div style={{
          fontFamily: 'Cinzel, serif', fontSize: 22, fontWeight: 700,
          color: BRASS.inkDeep, letterSpacing: 4, textTransform: 'uppercase',
          textShadow: '0 1px 0 rgba(253,228,154,0.6)',
        }}>{unit.name}</div>
      </div>
    </div>
  );
}

function BrassPlaque({ unit, items, states, side }) {
  const align = side === 'left' ? 'left' : 'right';
  return (
    <div style={{
      flex: 1,
      background: `
        linear-gradient(180deg, ${BRASS.parchment} 0%, ${BRASS.parchmentDeep} 100%)
      `,
      border: `2px solid ${BRASS.inkDeep}`,
      boxShadow: `
        inset 0 0 0 4px ${BRASS.parchment},
        inset 0 0 0 5px ${BRASS.brass},
        inset 0 0 60px rgba(122,90,32,0.25)
      `,
      position: 'relative',
      padding: '28px 32px',
    }}>
      {/* parchment texture */}
      <div style={{
        position: 'absolute', inset: 8, pointerEvents: 'none',
        backgroundImage: `
          radial-gradient(circle at 20% 30%, rgba(122,90,32,0.08), transparent 40%),
          radial-gradient(circle at 80% 70%, rgba(122,90,32,0.1), transparent 40%)
        `,
      }} />

      {/* heading */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 14,
        flexDirection: side === 'left' ? 'row' : 'row-reverse',
        borderBottom: `2px solid ${BRASS.brass}`,
        paddingBottom: 10, marginBottom: 8,
      }}>
        <div style={{
          fontFamily: 'Cinzel, serif', fontSize: 13, fontWeight: 600,
          color: BRASS.brassDeep, letterSpacing: 6, textTransform: 'uppercase',
        }}>Reliquary</div>
        <div style={{ flex: 1, height: 1, background: `repeating-linear-gradient(90deg, ${BRASS.brass} 0 4px, transparent 4px 8px)` }} />
        <div style={{
          fontFamily: 'Cormorant Garamond, serif', fontSize: 16, fontStyle: 'italic',
          color: BRASS.ink,
        }}>{items.filter(Boolean).length} of 5 carried</div>
      </div>

      <div style={{ position: 'relative' }}>
        {items.map((item, i) => {
          if (!item) {
            return (
              <div key={i} style={{
                height: 54, padding: '0 16px',
                display: 'flex', alignItems: 'center',
                justifyContent: align === 'left' ? 'flex-start' : 'flex-end',
                fontFamily: 'Cormorant Garamond, serif', fontStyle: 'italic',
                fontSize: 18, color: 'rgba(43,24,16,0.35)',
                borderTop: `1px dashed rgba(43,24,16,0.2)`,
              }}>— empty socket —</div>
            );
          }
          return (
            <BrassRow key={i}
              sigil={item.sigil} name={item.name} qty={item.qty}
              state={states[i]} align={align} />
          );
        })}
      </div>

      {/* weight meter */}
      <div style={{
        marginTop: 14, padding: '10px 14px',
        background: `linear-gradient(180deg, ${BRASS.parchmentDeep}, ${BRASS.parchmentShadow})`,
        border: `1px solid ${BRASS.inkDeep}`,
      }}>
        <div style={{
          display: 'flex', alignItems: 'center', gap: 10,
          flexDirection: side === 'left' ? 'row' : 'row-reverse',
        }}>
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 10, color: BRASS.inkDeep,
            letterSpacing: 3, textTransform: 'uppercase', minWidth: 60,
          }}>Burden</div>
          <div style={{ flex: 1, height: 10, background: BRASS.inkDeep, position: 'relative' }}>
            <div style={{
              position: 'absolute',
              [side === 'left' ? 'left' : 'right']: 0, top: 0, bottom: 0,
              width: '68%',
              background: `linear-gradient(90deg, ${BRASS.saffron}, ${BRASS.brassLite})`,
            }} />
            {[...Array(10)].map((_, i) => (
              <div key={i} style={{
                position: 'absolute', top: 0, bottom: 0, left: `${i * 10}%`,
                width: 1, background: 'rgba(0,0,0,0.5)',
              }} />
            ))}
          </div>
          <div style={{
            fontFamily: 'JetBrains Mono, monospace', fontSize: 12, color: BRASS.inkDeep,
            minWidth: 50, textAlign: 'right',
          }}>17/25</div>
        </div>
      </div>
    </div>
  );
}

function BrassButton({ label, icon, state, variant = 'default' }) {
  const isVerm = variant === 'vermillion';
  const base = isVerm
    ? { bg: BRASS.vermillion, bgHover: '#b83625', border: BRASS.brassLite, text: BRASS.parchment }
    : { bg: BRASS.brass, bgHover: BRASS.brassLite, border: BRASS.inkDeep, text: BRASS.inkDeep };
  const palette = {
    default:  { bg: `linear-gradient(180deg, ${base.bgHover}, ${base.bg})`, border: base.border, text: base.text, shadow: '0 3px 0 rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.3)', y: 0 },
    hover:    { bg: `linear-gradient(180deg, #fde49a, ${base.bgHover})`, border: base.border, text: base.text, shadow: '0 4px 0 rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.5)', y: -1 },
    pressed:  { bg: `linear-gradient(180deg, ${base.bg}, ${base.bgHover})`, border: base.border, text: base.text, shadow: '0 1px 0 rgba(0,0,0,0.4), inset 0 2px 4px rgba(0,0,0,0.4)', y: 2 },
    focused:  { bg: `linear-gradient(180deg, ${base.bgHover}, ${base.bg})`, border: BRASS.brassGlow, text: base.text, shadow: `0 0 0 3px rgba(253,228,154,0.6), 0 3px 0 rgba(0,0,0,0.4)`, y: 0 },
    disabled: { bg: `linear-gradient(180deg, rgba(184,136,53,0.35), rgba(122,90,32,0.35))`, border: 'rgba(43,24,16,0.3)', text: 'rgba(43,24,16,0.4)', shadow: 'none', y: 0 },
  };
  const s = palette[state];
  return (
    <div style={{ position: 'relative' }}>
      <div style={{
        height: 52, minWidth: 130,
        padding: '0 22px',
        display: 'flex', alignItems: 'center', gap: 10,
        background: s.bg,
        border: `2px solid ${s.border}`,
        boxShadow: s.shadow,
        transform: `translateY(${s.y}px)`,
      }}>
        {icon && <span style={{ fontFamily: 'Cinzel, serif', fontSize: 16, color: s.text }}>{icon}</span>}
        <span style={{
          fontFamily: 'Cinzel, serif', fontSize: 15, fontWeight: 700,
          color: s.text, letterSpacing: 3, textTransform: 'uppercase',
        }}>{label}</span>
      </div>
      <div style={{
        position: 'absolute', top: -18, left: 0, right: 0, textAlign: 'center',
        fontFamily: 'JetBrains Mono, monospace', fontSize: 9,
        color: BRASS.brassDeep, letterSpacing: 1, textTransform: 'uppercase',
      }}>{state}</div>
    </div>
  );
}

function VariantBrass() {
  const leftUnit = { name: 'Arjuna', epithet: 'Pandava Prince' };
  const rightUnit = { name: 'Bhima', epithet: 'Wind-Born' };
  const leftItems = [
    { sigil: 'arrow', name: 'Celestial Arrow', qty: 46 },
    { sigil: 'chakra', name: 'Sudarshan Disk', qty: 15 },
    { sigil: 'scroll', name: 'Vedic Scroll', qty: 3 },
    { sigil: 'gem', name: 'Kaustubha Gem', qty: 1 },
    null,
  ];
  const rightItems = [
    { sigil: 'trishul', name: 'Trishul Haft', qty: 30 },
    { sigil: 'lotus', name: 'Lotus Balm', qty: 20 },
    { sigil: 'flame', name: 'Agni Charm', qty: 8 },
    { sigil: 'conch', name: 'Broken Conch', qty: 2 },
    { sigil: 'shield', name: 'Iron Aegis', qty: 1 },
  ];
  const leftStates  = ['default', 'selected', 'hover', 'disabled', 'default'];
  const rightStates = ['focused', 'pressed', 'default', 'default', 'default'];

  return (
    <div style={{
      width: 1920, height: 1080,
      position: 'relative',
      fontFamily: 'EB Garamond, serif',
      color: BRASS.ink,
      overflow: 'hidden',
      background: `
        radial-gradient(ellipse at 50% 30%, #3a2f5a 0%, #1f1a3a 40%, #0a0612 100%)
      `,
    }}>
      {/* background ogee silhouettes (distant temple hall) */}
      <svg width="1920" height="1080" viewBox="0 0 1920 1080" style={{ position: 'absolute', inset: 0, opacity: 0.2 }}>
        {[0, 1, 2, 3, 4].map(i => {
          const x = 80 + i * 360;
          return (
            <path key={i}
              d={`M ${x} 900 L ${x} 500 Q ${x} 350 ${x + 70} 320 Q ${x + 120} 300 ${x + 160} 220 Q ${x + 200} 300 ${x + 250} 320 Q ${x + 320} 350 ${x + 320} 500 L ${x + 320} 900 Z`}
              fill="#0a0612" stroke={BRASS.brass} strokeWidth="1" />
          );
        })}
      </svg>

      {/* title bar */}
      <div style={{
        position: 'absolute', top: 24, left: 0, right: 0,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6,
      }}>
        <div style={{
          fontFamily: 'Cinzel, serif', fontSize: 28, fontWeight: 700,
          color: BRASS.brassLite, letterSpacing: 14, textTransform: 'uppercase',
          textShadow: '0 2px 4px rgba(0,0,0,0.8)',
        }}>Exchange</div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <PaisleyBorder width={180} height={14} color={BRASS.brass} opacity={0.9} />
          <div style={{ width: 8, height: 8, transform: 'rotate(45deg)', background: BRASS.brassLite }} />
          <PaisleyBorder width={180} height={14} color={BRASS.brass} opacity={0.9} />
        </div>
      </div>

      {/* main row: cameo | plaque | divider | plaque | cameo */}
      <div style={{
        position: 'absolute', top: 120, left: 40, right: 40, bottom: 140,
        display: 'flex', alignItems: 'stretch', gap: 16,
      }}>
        <div style={{ display: 'flex', alignItems: 'center' }}>
          <BrassCameo unit={leftUnit} side="left" />
        </div>

        <BrassPlaque unit={leftUnit} items={leftItems} states={leftStates} side="left" />

        {/* central exchange spindle */}
        <div style={{
          width: 70,
          display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
          gap: 16,
          background: `linear-gradient(180deg, ${BRASS.brassDeep}, ${BRASS.inkDeep})`,
          border: `2px solid ${BRASS.brass}`,
          padding: '20px 0',
          position: 'relative',
        }}>
          <LotusMedallion size={58} color={BRASS.brassLite} bg={BRASS.inkDeep} />
          <div style={{ display: 'flex', flexDirection: 'column', gap: 20, alignItems: 'center' }}>
            {/* double arrow motif */}
            <svg width="28" height="80" viewBox="0 0 28 80">
              <g stroke={BRASS.brassLite} strokeWidth="1.5" fill="none">
                <path d="M6 10 L 14 4 L 22 10" />
                <path d="M14 4 L 14 36" />
                <path d="M6 44 L 14 50 L 22 44" />
                <path d="M14 50 L 14 76" transform="translate(0, -26)" />
                <path d="M14 44 L 14 76" />
              </g>
            </svg>
          </div>
          <LotusMedallion size={58} color={BRASS.brassLite} bg={BRASS.inkDeep} />
        </div>

        <BrassPlaque unit={rightUnit} items={rightItems} states={rightStates} side="right" />

        <div style={{ display: 'flex', alignItems: 'center' }}>
          <BrassCameo unit={rightUnit} side="right" />
        </div>
      </div>

      {/* bottom action bar */}
      <div style={{
        position: 'absolute', bottom: 20, left: 40, right: 40, height: 100,
        background: `linear-gradient(180deg, ${BRASS.parchmentDeep}, ${BRASS.parchmentShadow})`,
        border: `2px solid ${BRASS.inkDeep}`,
        boxShadow: `inset 0 0 0 4px ${BRASS.parchment}, inset 0 0 0 5px ${BRASS.brass}`,
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '0 36px',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <div style={{
            width: 50, height: 50,
            background: BRASS.inkDeep,
            border: `2px solid ${BRASS.brassLite}`,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <ItemSigil type="chakra" color={BRASS.brassGlow} size={34} />
          </div>
          <div>
            <div style={{
              fontFamily: 'Cinzel, serif', fontSize: 10, color: BRASS.brassDeep,
              letterSpacing: 3, textTransform: 'uppercase',
            }}>Held in hand</div>
            <div style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: 28, fontStyle: 'italic', color: BRASS.inkDeep }}>
              Sudarshan Disk
              <span style={{ fontSize: 18, marginLeft: 14, color: BRASS.brassDeep, fontStyle: 'normal' }}>· uses: 15</span>
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', gap: 14 }}>
          <BrassButton label="Move" icon="◆" state="default" />
          <BrassButton label="Swap" icon="⇄" state="hover" />
          <BrassButton label="Place" icon="▼" state="pressed" />
          <BrassButton label="Inspect" icon="◎" state="focused" />
          <BrassButton label="Gift" icon="✦" state="disabled" />
          <BrassButton label="Depart" icon="⬢" state="default" variant="vermillion" />
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { VariantBrass });
