// Variant 1 — INDIGO CODEX
// Two facing pages of an illuminated manuscript. Central brass spine.
// Closest to the reference layout: portraits above, inventory panels below.

const CODEX = {
  parchment: '#f0e5c8',
  parchmentDark: '#e0d3b0',
  ink: '#1a1540',
  inkDeep: '#0f0b2e',
  brass: '#c9993a',
  brassLite: '#e8c66a',
  brassGlow: '#f5e0a0',
  vermillion: '#b0382a',
  jade: '#3a7a5a',
  wine: '#6b1e2e',
};

function CodexItemRow({ sigil, name, qty, state = 'default', align = 'left' }) {
  // state: default | hover | pressed | focused | selected | disabled
  const stateStyles = {
    default: { bg: 'transparent', border: 'transparent', text: CODEX.parchment, sigilColor: CODEX.brassLite, dim: false },
    hover:   { bg: 'rgba(232,198,106,0.10)', border: 'rgba(232,198,106,0.30)', text: CODEX.parchment, sigilColor: CODEX.brassLite, dim: false },
    pressed: { bg: 'rgba(232,198,106,0.22)', border: CODEX.brassLite, text: '#fff5d8', sigilColor: CODEX.brassGlow, dim: false, inset: true },
    focused: { bg: 'rgba(232,198,106,0.14)', border: CODEX.brassLite, text: CODEX.parchment, sigilColor: CODEX.brassLite, dim: false, glow: true },
    selected:{ bg: 'rgba(176,56,42,0.35)', border: CODEX.vermillion, text: '#fff5d8', sigilColor: CODEX.brassGlow, dim: false },
    disabled:{ bg: 'transparent', border: 'transparent', text: 'rgba(240,229,200,0.35)', sigilColor: CODEX.brass, dim: true },
  };
  const s = stateStyles[state];

  return (
    <div style={{
      display: 'grid',
      gridTemplateColumns: align === 'left' ? '44px 1fr 56px' : '56px 1fr 44px',
      alignItems: 'center',
      height: 52,
      padding: '0 14px',
      background: s.bg,
      border: `1px solid ${s.border}`,
      boxShadow: s.glow ? `0 0 0 2px rgba(245,224,160,0.25), inset 0 0 12px rgba(232,198,106,0.15)` : (s.inset ? 'inset 0 2px 4px rgba(0,0,0,0.4)' : 'none'),
      position: 'relative',
      transition: 'all 80ms linear',
    }}>
      {align === 'left' ? (
        <>
          <div style={{ display: 'flex', justifyContent: 'center' }}>
            <ItemSigil type={sigil} color={s.sigilColor} dim={s.dim} />
          </div>
          <div style={{
            fontFamily: 'Cormorant Garamond, serif', fontSize: 22, fontWeight: 500,
            color: s.text, letterSpacing: 0.3,
            textDecoration: s.dim ? 'line-through' : 'none',
            textDecorationColor: 'rgba(240,229,200,0.4)',
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
            color: s.text, letterSpacing: 0.3, textAlign: 'right',
            textDecoration: s.dim ? 'line-through' : 'none',
            textDecorationColor: 'rgba(240,229,200,0.4)',
          }}>{name}</div>
          <div style={{ display: 'flex', justifyContent: 'center' }}>
            <ItemSigil type={sigil} color={s.sigilColor} dim={s.dim} />
          </div>
        </>
      )}
      {/* selection marker */}
      {state === 'selected' && (
        <div style={{
          position: 'absolute',
          [align === 'left' ? 'left' : 'right']: -8, top: '50%',
          transform: 'translateY(-50%) rotate(45deg)',
          width: 10, height: 10, background: CODEX.vermillion,
          border: `1px solid ${CODEX.brassLite}`,
        }} />
      )}
      {/* focus caret */}
      {state === 'focused' && (
        <div style={{
          position: 'absolute',
          [align === 'left' ? 'left' : 'right']: 2,
          top: 8, bottom: 8, width: 2,
          background: CODEX.brassGlow,
          boxShadow: `0 0 6px ${CODEX.brassGlow}`,
        }} />
      )}
    </div>
  );
}

function CodexPanel({ unit, side, items, states }) {
  // items: [{sigil, name, qty}]
  // states: [default, hover, ...] per row
  const align = side === 'left' ? 'left' : 'right';
  return (
    <div style={{
      flex: 1,
      position: 'relative',
      background: `
        radial-gradient(ellipse at 50% 20%, rgba(232,198,106,0.06), transparent 60%),
        linear-gradient(180deg, #1f1a4a 0%, #0f0b2e 100%)
      `,
      border: `2px solid ${CODEX.brass}`,
      boxShadow: `inset 0 0 0 4px ${CODEX.inkDeep}, inset 0 0 0 5px ${CODEX.brass}, inset 0 0 40px rgba(0,0,0,0.5)`,
      padding: 28,
    }}>
      {/* corner filigrees */}
      <div style={{ position: 'absolute', top: 10, left: 10 }}><FiligreeCorner rotation={0} /></div>
      <div style={{ position: 'absolute', top: 10, right: 10 }}><FiligreeCorner rotation={90} /></div>
      <div style={{ position: 'absolute', bottom: 10, right: 10 }}><FiligreeCorner rotation={180} /></div>
      <div style={{ position: 'absolute', bottom: 10, left: 10 }}><FiligreeCorner rotation={270} /></div>

      {/* header */}
      <div style={{
        display: 'flex',
        flexDirection: side === 'left' ? 'row' : 'row-reverse',
        alignItems: 'center',
        gap: 24,
        marginBottom: 24,
      }}>
        <PortraitPlaceholder
          width={220} height={280}
          name={unit.name}
          epithet={unit.epithet}
          tone="wine"
        />
        <div style={{
          flex: 1,
          textAlign: side === 'left' ? 'left' : 'right',
        }}>
          <div style={{
            fontFamily: 'Cinzel, serif', fontSize: 14, fontWeight: 500,
            color: CODEX.brassLite, letterSpacing: 4, textTransform: 'uppercase',
            marginBottom: 8,
          }}>{unit.epithet}</div>
          <div style={{
            fontFamily: 'Cormorant Garamond, serif', fontSize: 56, fontWeight: 600,
            color: CODEX.parchment, letterSpacing: 0.5, lineHeight: 1,
            fontStyle: 'italic',
          }}>{unit.name}</div>
          <div style={{
            marginTop: 12,
            display: 'flex',
            flexDirection: side === 'left' ? 'row' : 'row-reverse',
            gap: 18,
            fontFamily: 'Cormorant Garamond, serif', fontSize: 16,
            color: 'rgba(240,229,200,0.75)',
          }}>
            <div><span style={{ color: CODEX.brassLite, fontFamily: 'Cinzel, serif', fontSize: 11, letterSpacing: 2 }}>CLASS </span>{unit.cls}</div>
            <div><span style={{ color: CODEX.brassLite, fontFamily: 'Cinzel, serif', fontSize: 11, letterSpacing: 2 }}>LV </span>{unit.lv}</div>
            <div><span style={{ color: CODEX.brassLite, fontFamily: 'Cinzel, serif', fontSize: 11, letterSpacing: 2 }}>CARRY </span>{items.filter(Boolean).length}/5</div>
          </div>
          {/* paisley divider */}
          <div style={{ marginTop: 14, display: 'flex', justifyContent: side === 'left' ? 'flex-start' : 'flex-end' }}>
            <PaisleyBorder width={240} height={14} color={CODEX.brass} opacity={0.7} />
          </div>
        </div>
      </div>

      {/* inventory heading */}
      <div style={{
        fontFamily: 'Cinzel, serif', fontSize: 12, fontWeight: 500,
        color: CODEX.brassLite, letterSpacing: 5, textTransform: 'uppercase',
        textAlign: align, marginBottom: 10,
        borderBottom: `1px solid ${CODEX.brass}`, paddingBottom: 6,
      }}>
        {side === 'left' ? '◆  Satchel' : 'Satchel  ◆'}
      </div>

      {/* inventory rows */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
        {items.map((item, i) => {
          if (!item) {
            return (
              <div key={i} style={{
                height: 52, padding: '0 14px',
                display: 'flex', alignItems: 'center',
                justifyContent: align === 'left' ? 'flex-start' : 'flex-end',
                fontFamily: 'Cormorant Garamond, serif', fontStyle: 'italic',
                fontSize: 18, color: 'rgba(240,229,200,0.25)',
                borderTop: '1px dashed rgba(232,198,106,0.2)',
              }}>— empty —</div>
            );
          }
          return (
            <CodexItemRow key={i}
              sigil={item.sigil} name={item.name} qty={item.qty}
              state={states[i]} align={align} />
          );
        })}
      </div>
    </div>
  );
}

function VariantCodex() {
  const leftUnit = { name: 'Arjuna', epithet: 'Pandava Prince', cls: 'Dhanurveda', lv: 14 };
  const rightUnit = { name: 'Bhima', epithet: 'Wind-Born', cls: 'Gada-dhara', lv: 16 };

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

  // Row states to showcase all interactive states across the screen:
  // Left:  default, hover, selected, disabled, (empty)
  // Right: focused, pressed, default, default, default
  const leftStates  = ['default', 'hover', 'selected', 'disabled', 'default'];
  const rightStates = ['focused', 'pressed', 'default', 'default', 'default'];

  return (
    <div style={{
      width: 1920, height: 1080,
      position: 'relative',
      fontFamily: 'EB Garamond, serif',
      color: CODEX.parchment,
      overflow: 'hidden',
      background: `
        radial-gradient(ellipse at 50% 20%, rgba(60,40,100,0.7), transparent 60%),
        radial-gradient(ellipse at 50% 100%, rgba(20,10,40,1), transparent 60%),
        linear-gradient(180deg, #0b0820 0%, #05030f 100%)
      `,
    }}>
      {/* distant ornamental backdrop: arches */}
      <svg width="1920" height="1080" viewBox="0 0 1920 1080" style={{ position: 'absolute', inset: 0, opacity: 0.08 }}>
        <defs>
          <pattern id="codex-dots" x="0" y="0" width="40" height="40" patternUnits="userSpaceOnUse">
            <circle cx="20" cy="20" r="0.8" fill={CODEX.brassLite} />
          </pattern>
        </defs>
        <rect width="1920" height="1080" fill="url(#codex-dots)" />
      </svg>

      {/* top banner */}
      <div style={{
        position: 'absolute', top: 28, left: 0, right: 0,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8,
      }}>
        <div style={{
          fontFamily: 'Cinzel, serif', fontSize: 13, fontWeight: 500,
          color: CODEX.brassLite, letterSpacing: 10, textTransform: 'uppercase',
        }}>Parley  ·  Exchange of Provisions</div>
        <PaisleyBorder width={520} height={16} color={CODEX.brass} opacity={0.8} />
      </div>

      {/* main content */}
      <div style={{
        position: 'absolute', top: 96, left: 56, right: 56, bottom: 140,
        display: 'flex', gap: 0,
      }}>
        <CodexPanel unit={leftUnit} side="left" items={leftItems} states={leftStates} />

        {/* center spine */}
        <div style={{
          width: 80, position: 'relative',
          display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
          background: `linear-gradient(180deg, ${CODEX.inkDeep}, #1a1540, ${CODEX.inkDeep})`,
          borderTop: `2px solid ${CODEX.brass}`,
          borderBottom: `2px solid ${CODEX.brass}`,
        }}>
          <div style={{
            position: 'absolute', inset: 0,
            backgroundImage: `repeating-linear-gradient(0deg, transparent 0 14px, rgba(232,198,106,0.15) 14px 15px)`,
          }} />
          <LotusMedallion size={72} color={CODEX.brassLite} />
          <div style={{ marginTop: 14 }}>
            {[0,1,2].map(i => (
              <div key={i} style={{
                width: 6, height: 6, borderRadius: 0, transform: 'rotate(45deg)',
                background: CODEX.brass, margin: '4px auto',
              }} />
            ))}
          </div>
          <LotusMedallion size={72} color={CODEX.brassLite} />
        </div>

        <CodexPanel unit={rightUnit} side="right" items={rightItems} states={rightStates} />
      </div>

      {/* bottom action bar */}
      <div style={{
        position: 'absolute', bottom: 24, left: 56, right: 56,
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '18px 32px',
        background: `linear-gradient(180deg, rgba(26,21,64,0.8), rgba(15,11,46,0.95))`,
        border: `1.5px solid ${CODEX.brass}`,
        boxShadow: `inset 0 0 0 3px ${CODEX.inkDeep}, inset 0 0 0 4px rgba(232,198,106,0.5)`,
      }}>
        {/* selection readout */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <div style={{
            width: 10, height: 10, transform: 'rotate(45deg)',
            background: CODEX.vermillion, border: `1px solid ${CODEX.brassLite}`,
          }} />
          <div style={{ fontFamily: 'Cinzel, serif', fontSize: 11, letterSpacing: 3, color: CODEX.brassLite }}>HOLDING</div>
          <div style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: 24, fontStyle: 'italic', color: CODEX.parchment }}>
            Sudarshan Disk <span style={{ color: CODEX.brassLite }}>· 15 uses</span>
          </div>
        </div>

        {/* button row — shows default, hover, pressed, focused, disabled */}
        <div style={{ display: 'flex', gap: 12 }}>
          <CodexButton label="Move" shortcut="A" state="default" />
          <CodexButton label="Swap" shortcut="S" state="hover" />
          <CodexButton label="Place" shortcut="D" state="pressed" />
          <CodexButton label="Inspect" shortcut="I" state="focused" />
          <CodexButton label="Gift" shortcut="G" state="disabled" />
          <CodexButton label="Conclude" shortcut="B" state="default" variant="vermillion" />
        </div>
      </div>
    </div>
  );
}

function CodexButton({ label, shortcut, state, variant = 'default' }) {
  const isVerm = variant === 'vermillion';
  const palette = {
    default:  { bg: isVerm ? 'rgba(176,56,42,0.4)' : 'rgba(26,21,64,0.6)', border: CODEX.brass, text: CODEX.parchment, glow: false, inset: false },
    hover:    { bg: isVerm ? 'rgba(176,56,42,0.6)' : 'rgba(232,198,106,0.15)', border: CODEX.brassLite, text: '#fff5d8', glow: false, inset: false },
    pressed:  { bg: isVerm ? 'rgba(120,30,20,0.9)' : 'rgba(232,198,106,0.3)', border: CODEX.brassLite, text: '#fff5d8', glow: false, inset: true },
    focused:  { bg: isVerm ? 'rgba(176,56,42,0.45)' : 'rgba(26,21,64,0.7)', border: CODEX.brassGlow, text: CODEX.parchment, glow: true, inset: false },
    disabled: { bg: 'rgba(26,21,64,0.3)', border: 'rgba(201,153,58,0.3)', text: 'rgba(240,229,200,0.3)', glow: false, inset: false },
  };
  const s = palette[state];
  return (
    <div style={{
      minWidth: 110, height: 44,
      display: 'flex', alignItems: 'center', gap: 10,
      padding: '0 18px',
      background: s.bg,
      border: `1.5px solid ${s.border}`,
      boxShadow: s.glow
        ? `0 0 0 2px rgba(245,224,160,0.35), inset 0 0 12px rgba(232,198,106,0.18)`
        : (s.inset ? 'inset 0 2px 6px rgba(0,0,0,0.5)' : 'inset 0 0 0 2px rgba(0,0,0,0.3)'),
      position: 'relative',
    }}>
      <span style={{
        fontFamily: 'Cinzel, serif', fontSize: 10, fontWeight: 600,
        color: s.border, letterSpacing: 1,
        width: 18, height: 18, display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        border: `1px solid ${s.border}`, borderRadius: '50%',
      }}>{shortcut}</span>
      <span style={{
        fontFamily: 'Cormorant Garamond, serif', fontSize: 20, fontWeight: 500,
        color: s.text, letterSpacing: 0.5,
      }}>{label}</span>
      <span style={{
        position: 'absolute', top: -8, right: -8,
        fontFamily: 'JetBrains Mono, monospace', fontSize: 8,
        color: 'rgba(201,153,58,0.6)', letterSpacing: 0.5,
        textTransform: 'uppercase',
      }}>{state}</span>
    </div>
  );
}

Object.assign(window, { VariantCodex });
