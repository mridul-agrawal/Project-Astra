// Variant 3 — MANDALA CIRCLE
// Breaks the two-column mold: portraits as circular cameos, inventories
// curve around them in a concentric mandala. Central "offering bowl"
// hosts the selected item. More experimental layout.

const MAND = {
  night: '#0a0820',
  deepBlue: '#14122e',
  indigo: '#1e1a4a',
  parchment: '#f2e6c4',
  parchDim: '#c9b98a',
  brass: '#d4a64a',
  brassLite: '#f0cf7a',
  brassGlow: '#ffe8a8',
  brassDeep: '#7a5820',
  vermillion: '#c03a24',
  saffron: '#e6821a',
  jade: '#3a8868',
  lotus: '#e8d9a8',
};

function MandalaRow({ sigil, name, qty, state, side }) {
  const align = side === 'left' ? 'left' : 'right';
  const palette = {
    default: { bg: 'rgba(20,18,46,0.7)', border: 'rgba(212,166,74,0.35)', text: MAND.parchment, sigilColor: MAND.brass },
    hover:   { bg: 'rgba(30,26,74,0.85)', border: MAND.brass, text: MAND.parchment, sigilColor: MAND.brassLite, shadow: '0 0 16px rgba(212,166,74,0.25)' },
    pressed: { bg: 'rgba(10,8,30,0.95)', border: MAND.brass, text: MAND.parchment, sigilColor: MAND.brassLite, shadow: 'inset 0 3px 6px rgba(0,0,0,0.7)' },
    focused: { bg: 'rgba(30,26,74,0.85)', border: MAND.brassGlow, text: '#fff', sigilColor: MAND.brassGlow, shadow: `0 0 0 2px ${MAND.brassGlow}, 0 0 20px rgba(255,232,168,0.4)` },
    selected:{ bg: `linear-gradient(90deg, rgba(192,58,36,0.6), rgba(230,130,26,0.4))`, border: MAND.brassGlow, text: '#fff5d8', sigilColor: MAND.brassGlow, shadow: `0 0 20px rgba(230,130,26,0.5)` },
    disabled:{ bg: 'rgba(20,18,46,0.3)', border: 'rgba(212,166,74,0.1)', text: 'rgba(242,230,196,0.3)', sigilColor: 'rgba(122,88,32,0.5)', dim: true },
  };
  const s = palette[state];

  return (
    <div style={{
      position: 'relative',
      display: 'grid',
      gridTemplateColumns: align === 'left' ? '44px 1fr 60px 12px' : '12px 60px 1fr 44px',
      alignItems: 'center',
      height: 56, padding: '0 18px',
      background: s.bg,
      border: `1.5px solid ${s.border}`,
      borderRadius: align === 'left' ? '28px 4px 4px 28px' : '4px 28px 28px 4px',
      boxShadow: s.shadow || 'none',
      backdropFilter: 'blur(2px)',
    }}>
      {align === 'left' ? (
        <>
          <div style={{
            width: 38, height: 38, borderRadius: '50%',
            background: state === 'selected' ? MAND.vermillion : MAND.deepBlue,
            border: `1.5px solid ${s.sigilColor}`,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <ItemSigil type={sigil} color={s.sigilColor} dim={s.dim} size={24} />
          </div>
          <div style={{
            fontFamily: 'Cormorant Garamond, serif', fontSize: 23, fontWeight: 500,
            color: s.text, letterSpacing: 0.3,
            textDecoration: s.dim ? 'line-through' : 'none',
          }}>{name}</div>
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 18, fontWeight: 600,
            color: s.text, textAlign: 'right', fontVariantNumeric: 'tabular-nums',
          }}>×{qty}</div>
          <div />
        </>
      ) : (
        <>
          <div />
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 18, fontWeight: 600,
            color: s.text, textAlign: 'left', fontVariantNumeric: 'tabular-nums',
          }}>×{qty}</div>
          <div style={{
            fontFamily: 'Cormorant Garamond, serif', fontSize: 23, fontWeight: 500,
            color: s.text, letterSpacing: 0.3, textAlign: 'right',
            textDecoration: s.dim ? 'line-through' : 'none',
          }}>{name}</div>
          <div style={{
            width: 38, height: 38, borderRadius: '50%',
            background: state === 'selected' ? MAND.vermillion : MAND.deepBlue,
            border: `1.5px solid ${s.sigilColor}`,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <ItemSigil type={sigil} color={s.sigilColor} dim={s.dim} size={24} />
          </div>
        </>
      )}
      {state === 'selected' && (
        <div style={{
          position: 'absolute',
          [align === 'left' ? 'right' : 'left']: -18, top: '50%',
          transform: 'translateY(-50%)',
          display: 'flex', alignItems: 'center', gap: 2,
        }}>
          <div style={{ width: 14, height: 2, background: MAND.brassGlow, boxShadow: `0 0 8px ${MAND.brassGlow}` }} />
          <div style={{ width: 6, height: 6, borderRadius: '50%', background: MAND.brassGlow, boxShadow: `0 0 8px ${MAND.brassGlow}` }} />
        </div>
      )}
    </div>
  );
}

function MandalaPortrait({ unit, side }) {
  const ring = 30;
  return (
    <div style={{ position: 'relative', width: 320, height: 320 }}>
      {/* outer ring of petals */}
      <svg width="320" height="320" viewBox="0 0 320 320" style={{ position: 'absolute', inset: 0 }}>
        <defs>
          <radialGradient id={`mand-glow-${side}`}>
            <stop offset="0%" stopColor="rgba(212,166,74,0.4)" />
            <stop offset="100%" stopColor="rgba(212,166,74,0)" />
          </radialGradient>
        </defs>
        <circle cx="160" cy="160" r="158" fill={`url(#mand-glow-${side})`} />
        {/* 16 petals */}
        {[...Array(16)].map((_, i) => {
          const a = (i * 360) / 16;
          return (
            <path key={i}
              d="M 160 160 Q 165 60 160 10 Q 155 60 160 160 Z"
              fill={MAND.brass} opacity="0.85"
              transform={`rotate(${a} 160 160)`} />
          );
        })}
        <circle cx="160" cy="160" r="135" fill={MAND.deepBlue} stroke={MAND.brassLite} strokeWidth="2" />
        <circle cx="160" cy="160" r="125" fill="none" stroke={MAND.brass} strokeWidth="1" strokeDasharray="2 4" />
        {/* small dots around */}
        {[...Array(32)].map((_, i) => {
          const a = (i * Math.PI * 2) / 32;
          const r = 140;
          return <circle key={i} cx={160 + Math.cos(a) * r} cy={160 + Math.sin(a) * r} r="1.2" fill={MAND.brassLite} />;
        })}
      </svg>
      {/* portrait in circle */}
      <div style={{
        position: 'absolute', top: 35, left: 35, right: 35, bottom: 35,
        borderRadius: '50%', overflow: 'hidden',
        border: `2px solid ${MAND.brassDeep}`,
      }}>
        <PortraitPlaceholder width={250} height={250} name={unit.name} epithet={unit.epithet} tone="indigo" />
      </div>
      {/* name ribbon */}
      <div style={{
        position: 'absolute', bottom: -12, left: '50%', transform: 'translateX(-50%)',
        background: `linear-gradient(180deg, ${MAND.brassLite}, ${MAND.brass})`,
        border: `1.5px solid ${MAND.brassDeep}`,
        padding: '6px 22px', minWidth: 180, textAlign: 'center',
        clipPath: 'polygon(8% 0, 92% 0, 100% 50%, 92% 100%, 8% 100%, 0 50%)',
      }}>
        <div style={{
          fontFamily: 'Cinzel, serif', fontSize: 18, fontWeight: 700,
          color: MAND.night, letterSpacing: 5, textTransform: 'uppercase',
        }}>{unit.name}</div>
      </div>
    </div>
  );
}

function MandalaButton({ label, state, variant = 'default', shortcut }) {
  const isVerm = variant === 'vermillion';
  const palette = {
    default:  { bg: isVerm ? 'rgba(192,58,36,0.3)' : 'rgba(20,18,46,0.7)', border: MAND.brass, text: MAND.parchment, shadow: 'none' },
    hover:    { bg: isVerm ? 'rgba(192,58,36,0.5)' : 'rgba(30,26,74,0.9)', border: MAND.brassLite, text: '#fff5d8', shadow: `0 0 14px rgba(212,166,74,0.35)` },
    pressed:  { bg: isVerm ? 'rgba(120,30,15,0.9)' : 'rgba(8,6,20,0.95)', border: MAND.brass, text: MAND.parchment, shadow: 'inset 0 3px 8px rgba(0,0,0,0.7)' },
    focused:  { bg: isVerm ? 'rgba(192,58,36,0.4)' : 'rgba(20,18,46,0.8)', border: MAND.brassGlow, text: '#fff', shadow: `0 0 0 2px ${MAND.brassGlow}, 0 0 18px rgba(255,232,168,0.4)` },
    disabled: { bg: 'rgba(20,18,46,0.2)', border: 'rgba(212,166,74,0.2)', text: 'rgba(242,230,196,0.3)', shadow: 'none' },
  };
  const s = palette[state];
  return (
    <div style={{ position: 'relative' }}>
      <div style={{
        height: 48, minWidth: 128, padding: '0 20px',
        borderRadius: 24,
        background: s.bg, border: `1.5px solid ${s.border}`,
        boxShadow: s.shadow,
        display: 'flex', alignItems: 'center', gap: 10,
        justifyContent: 'center',
      }}>
        {shortcut && (
          <span style={{
            width: 22, height: 22, borderRadius: '50%',
            border: `1px solid ${s.border}`, color: s.border,
            fontFamily: 'JetBrains Mono, monospace', fontSize: 10, fontWeight: 600,
            display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
          }}>{shortcut}</span>
        )}
        <span style={{
          fontFamily: 'Cinzel, serif', fontSize: 13, fontWeight: 600,
          color: s.text, letterSpacing: 3, textTransform: 'uppercase',
        }}>{label}</span>
      </div>
      <div style={{
        position: 'absolute', top: -16, left: 0, right: 0, textAlign: 'center',
        fontFamily: 'JetBrains Mono, monospace', fontSize: 9,
        color: MAND.brass, letterSpacing: 1, textTransform: 'uppercase', opacity: 0.7,
      }}>{state}</div>
    </div>
  );
}

function VariantMandala() {
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
  const leftStates  = ['hover', 'selected', 'default', 'disabled', 'default'];
  const rightStates = ['default', 'focused', 'pressed', 'default', 'default'];

  return (
    <div style={{
      width: 1920, height: 1080,
      position: 'relative',
      fontFamily: 'EB Garamond, serif',
      color: MAND.parchment,
      overflow: 'hidden',
      background: `
        radial-gradient(ellipse at 50% 50%, ${MAND.indigo} 0%, ${MAND.deepBlue} 40%, ${MAND.night} 100%)
      `,
    }}>
      {/* ambient starry constellation background */}
      <svg width="1920" height="1080" viewBox="0 0 1920 1080" style={{ position: 'absolute', inset: 0, opacity: 0.5 }}>
        {[...Array(80)].map((_, i) => {
          const x = (i * 137) % 1920;
          const y = ((i * 71) % 1080);
          const r = (i % 3) * 0.4 + 0.4;
          return <circle key={i} cx={x} cy={y} r={r} fill={MAND.brassLite} opacity={0.3 + (i % 5) * 0.1} />;
        })}
      </svg>

      {/* giant mandala behind everything */}
      <svg width="1400" height="1400" viewBox="0 0 1400 1400" style={{
        position: 'absolute', left: '50%', top: '50%',
        transform: 'translate(-50%, -50%)', opacity: 0.18,
      }}>
        {[...Array(24)].map((_, i) => {
          const a = (i * 360) / 24;
          return (
            <path key={i}
              d="M 700 700 Q 720 300 700 40 Q 680 300 700 700 Z"
              fill="none" stroke={MAND.brassLite} strokeWidth="1"
              transform={`rotate(${a} 700 700)`} />
          );
        })}
        <circle cx="700" cy="700" r="620" fill="none" stroke={MAND.brassLite} strokeWidth="1" />
        <circle cx="700" cy="700" r="520" fill="none" stroke={MAND.brassLite} strokeWidth="0.5" strokeDasharray="3 6" />
        <circle cx="700" cy="700" r="420" fill="none" stroke={MAND.brassLite} strokeWidth="1" />
      </svg>

      {/* title */}
      <div style={{
        position: 'absolute', top: 28, left: 0, right: 0,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 18 }}>
          <div style={{ width: 120, height: 1, background: `linear-gradient(90deg, transparent, ${MAND.brass})` }} />
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 24, fontWeight: 600,
            color: MAND.brassLite, letterSpacing: 16, textTransform: 'uppercase',
          }}>सन्धि · Sandhi</div>
          <div style={{ width: 120, height: 1, background: `linear-gradient(90deg, ${MAND.brass}, transparent)` }} />
        </div>
        <div style={{
          fontFamily: 'Cormorant Garamond, serif', fontSize: 16, fontStyle: 'italic',
          color: MAND.parchDim, letterSpacing: 1.5,
        }}>the rite of exchange between companions</div>
      </div>

      {/* left column: portrait + inventory */}
      <div style={{
        position: 'absolute', top: 160, left: 60, width: 640,
        display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 32,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
          <MandalaPortrait unit={leftUnit} side="left" />
          <div>
            <div style={{
              fontFamily: 'Cinzel, serif', fontSize: 11,
              color: MAND.brassLite, letterSpacing: 4, textTransform: 'uppercase',
              marginBottom: 4,
            }}>{leftUnit.epithet}</div>
            <div style={{
              display: 'flex', gap: 14, fontFamily: 'Cormorant Garamond, serif', fontSize: 15,
              color: MAND.parchDim,
            }}>
              <div><span style={{ color: MAND.brassLite }}>LV</span> 14</div>
              <div><span style={{ color: MAND.brassLite }}>HP</span> 32/32</div>
              <div><span style={{ color: MAND.brassLite }}>WT</span> 17/25</div>
            </div>
          </div>
        </div>
        <div style={{
          width: '100%', display: 'flex', flexDirection: 'column', gap: 8,
        }}>
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 11, color: MAND.brassLite,
            letterSpacing: 5, textTransform: 'uppercase', marginBottom: 4,
          }}>◆ Provisions held</div>
          {leftItems.map((item, i) => item
            ? <MandalaRow key={i} {...item} state={leftStates[i]} side="left" />
            : <div key={i} style={{
                height: 56, padding: '0 18px', borderRadius: '28px 4px 4px 28px',
                border: '1.5px dashed rgba(212,166,74,0.2)',
                display: 'flex', alignItems: 'center',
                fontFamily: 'Cormorant Garamond, serif', fontStyle: 'italic',
                fontSize: 18, color: 'rgba(242,230,196,0.3)',
              }}>— empty vessel —</div>
          )}
        </div>
      </div>

      {/* right column */}
      <div style={{
        position: 'absolute', top: 160, right: 60, width: 640,
        display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 32,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 24, flexDirection: 'row-reverse' }}>
          <MandalaPortrait unit={rightUnit} side="right" />
          <div style={{ textAlign: 'right' }}>
            <div style={{
              fontFamily: 'Cinzel, serif', fontSize: 11,
              color: MAND.brassLite, letterSpacing: 4, textTransform: 'uppercase',
              marginBottom: 4,
            }}>{rightUnit.epithet}</div>
            <div style={{
              display: 'flex', gap: 14, justifyContent: 'flex-end',
              fontFamily: 'Cormorant Garamond, serif', fontSize: 15, color: MAND.parchDim,
            }}>
              <div><span style={{ color: MAND.brassLite }}>LV</span> 16</div>
              <div><span style={{ color: MAND.brassLite }}>HP</span> 41/41</div>
              <div><span style={{ color: MAND.brassLite }}>WT</span> 22/28</div>
            </div>
          </div>
        </div>
        <div style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: 8 }}>
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 11, color: MAND.brassLite,
            letterSpacing: 5, textTransform: 'uppercase', textAlign: 'right', marginBottom: 4,
          }}>Provisions held ◆</div>
          {rightItems.map((item, i) => item
            ? <MandalaRow key={i} {...item} state={rightStates[i]} side="right" />
            : null
          )}
        </div>
      </div>

      {/* center offering bowl — holds currently grabbed item */}
      <div style={{
        position: 'absolute', left: '50%', top: 410,
        transform: 'translateX(-50%)',
        width: 220, height: 280,
        display: 'flex', flexDirection: 'column', alignItems: 'center',
      }}>
        {/* floating held item */}
        <div style={{
          width: 140, height: 140, borderRadius: '50%',
          background: `radial-gradient(circle, ${MAND.vermillion} 0%, ${MAND.night} 70%)`,
          border: `2px solid ${MAND.brassGlow}`,
          boxShadow: `0 0 40px rgba(230,130,26,0.5), inset 0 0 20px rgba(0,0,0,0.6)`,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          position: 'relative',
        }}>
          <ItemSigil type="chakra" color={MAND.brassGlow} size={72} />
          {/* orbiting dots */}
          <svg width="140" height="140" viewBox="0 0 140 140" style={{ position: 'absolute', inset: 0 }}>
            {[...Array(8)].map((_, i) => {
              const a = (i * Math.PI * 2) / 8;
              return <circle key={i} cx={70 + Math.cos(a) * 62} cy={70 + Math.sin(a) * 62} r="2" fill={MAND.brassGlow} />;
            })}
          </svg>
        </div>
        {/* flowing brass curtain */}
        <svg width="220" height="120" viewBox="0 0 220 120" style={{ marginTop: 4 }}>
          <path d="M 30 0 Q 110 30 190 0 Q 180 30 190 60 Q 110 90 30 60 Q 40 30 30 0 Z"
            fill={`url(#bowl-grad)`} stroke={MAND.brassDeep} strokeWidth="1.5" />
          <defs>
            <linearGradient id="bowl-grad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={MAND.brassLite} />
              <stop offset="100%" stopColor={MAND.brassDeep} />
            </linearGradient>
          </defs>
          <path d="M 40 20 Q 110 40 180 20" stroke={MAND.brassDeep} strokeWidth="1" fill="none" opacity="0.6" />
        </svg>
        <div style={{
          fontFamily: 'Cinzel, serif', fontSize: 11,
          color: MAND.brassLite, letterSpacing: 4, textTransform: 'uppercase',
          marginTop: 10,
        }}>In Hand</div>
        <div style={{
          fontFamily: 'Cormorant Garamond, serif', fontSize: 26, fontStyle: 'italic',
          color: MAND.parchment, marginTop: 2,
        }}>Sudarshan Disk</div>
        <div style={{
          fontFamily: 'JetBrains Mono, monospace', fontSize: 11,
          color: MAND.brassLite, marginTop: 2,
        }}>15 uses · legendary</div>
      </div>

      {/* bottom action ring */}
      <div style={{
        position: 'absolute', bottom: 32, left: 0, right: 0,
        display: 'flex', justifyContent: 'center', gap: 12,
      }}>
        <MandalaButton label="Move" shortcut="A" state="default" />
        <MandalaButton label="Swap" shortcut="S" state="hover" />
        <MandalaButton label="Place" shortcut="D" state="pressed" />
        <MandalaButton label="Inspect" shortcut="I" state="focused" />
        <MandalaButton label="Gift" shortcut="G" state="disabled" />
        <MandalaButton label="Conclude" shortcut="B" state="default" variant="vermillion" />
      </div>

      {/* footer hint */}
      <div style={{
        position: 'absolute', bottom: 10, left: 0, right: 0, textAlign: 'center',
        fontFamily: 'JetBrains Mono, monospace', fontSize: 10,
        color: MAND.parchDim, letterSpacing: 2, opacity: 0.6,
      }}>▲ ▼ select item   ◀ ▶ switch companion   enter confirm   esc depart</div>
    </div>
  );
}

Object.assign(window, { VariantMandala });
