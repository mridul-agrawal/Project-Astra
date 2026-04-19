// Shared ornament primitives for Project Astra trade screens.
// All ornaments are geometric/abstract — no figurative illustration.

// Filigree corner — four-way rotatable brass corner flourish
function FiligreeCorner({ size = 48, color = '#c9993a', rotation = 0, opacity = 1 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 48 48"
      style={{ transform: `rotate(${rotation}deg)`, opacity, display: 'block' }}>
      <g fill="none" stroke={color} strokeWidth="1.2" strokeLinecap="round">
        <path d="M0 4 L4 4 L4 0" />
        <path d="M6 6 Q 14 6 14 14 Q 14 22 22 22" />
        <path d="M8 8 Q 12 8 12 12 Q 12 16 16 16" opacity="0.6" />
        <circle cx="22" cy="22" r="1.6" fill={color} stroke="none" />
        <path d="M4 10 Q 8 10 8 14" opacity="0.7" />
      </g>
    </svg>
  );
}

// Lotus medallion — stylized 8-petal mandala
function LotusMedallion({ size = 64, color = '#e8c66a', bg = 'transparent', strokeWidth = 1 }) {
  const petals = [];
  for (let i = 0; i < 8; i++) {
    const a = (i * 360) / 8;
    petals.push(
      <path key={i} d="M 32 32 Q 34 18 32 8 Q 30 18 32 32 Z"
        fill={color} opacity="0.9"
        transform={`rotate(${a} 32 32)`} />
    );
  }
  return (
    <svg width={size} height={size} viewBox="0 0 64 64" style={{ display: 'block' }}>
      {bg !== 'transparent' && <circle cx="32" cy="32" r="30" fill={bg} />}
      <g stroke={color} strokeWidth={strokeWidth} fill="none" opacity="0.5">
        <circle cx="32" cy="32" r="28" />
        <circle cx="32" cy="32" r="22" />
      </g>
      {petals}
      <circle cx="32" cy="32" r="5" fill={color} />
      <circle cx="32" cy="32" r="2" fill={bg === 'transparent' ? '#1a1540' : bg} />
    </svg>
  );
}

// Paisley border repeat — horizontal band
function PaisleyBorder({ width = 400, height = 24, color = '#c9993a', opacity = 0.9 }) {
  const motifs = Math.floor(width / 48);
  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`} style={{ display: 'block' }}>
      <line x1="0" y1={height / 2} x2={width} y2={height / 2} stroke={color} strokeWidth="0.5" opacity="0.4" />
      {[...Array(motifs)].map((_, i) => {
        const x = 24 + i * 48;
        return (
          <g key={i} stroke={color} fill="none" strokeWidth="1" opacity={opacity}>
            <path d={`M ${x - 8} ${height / 2} Q ${x - 8} ${height / 2 - 6} ${x} ${height / 2 - 6} Q ${x + 8} ${height / 2 - 6} ${x + 8} ${height / 2} Q ${x + 8} ${height / 2 + 6} ${x} ${height / 2 + 6} Q ${x - 8} ${height / 2 + 6} ${x - 8} ${height / 2}`} />
            <circle cx={x} cy={height / 2} r="1.2" fill={color} />
          </g>
        );
      })}
    </svg>
  );
}

// Ogee arch silhouette — temple doorway shape
function OgeeArch({ width = 200, height = 280, fill = '#0f0b2e', stroke = '#c9993a', strokeWidth = 2, children, innerPad = 12 }) {
  // Pointed-lobed arch. Two S-curves meeting at top.
  const w = width, h = height;
  const shoulder = h * 0.35;
  const d = `
    M 0 ${h}
    L 0 ${shoulder}
    Q 0 ${shoulder * 0.4} ${w * 0.25} ${shoulder * 0.35}
    Q ${w * 0.5} ${shoulder * 0.3} ${w * 0.5} 0
    Q ${w * 0.5} ${shoulder * 0.3} ${w * 0.75} ${shoulder * 0.35}
    Q ${w} ${shoulder * 0.4} ${w} ${shoulder}
    L ${w} ${h}
    Z
  `;
  return (
    <div style={{ position: 'relative', width, height }}>
      <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`} style={{ display: 'block', position: 'absolute', inset: 0 }}>
        <path d={d} fill={fill} stroke={stroke} strokeWidth={strokeWidth} />
      </svg>
      <div style={{ position: 'absolute', inset: innerPad, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        {children}
      </div>
    </div>
  );
}

// Item emblem — abstract brass-line sigil for an inventory item type
// Each sigil is geometric; no literal weapon illustrations.
function ItemSigil({ type, size = 28, color = '#e8c66a', dim = false }) {
  const sw = 1.4;
  const op = dim ? 0.4 : 1;
  const s = {
    chakra: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <circle cx="16" cy="16" r="10" />
        <circle cx="16" cy="16" r="5" />
        {[...Array(8)].map((_, i) => {
          const a = (i * Math.PI) / 4;
          return <line key={i}
            x1={16 + Math.cos(a) * 5} y1={16 + Math.sin(a) * 5}
            x2={16 + Math.cos(a) * 10} y2={16 + Math.sin(a) * 10} />;
        })}
      </g>
    ),
    trishul: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M16 4 L16 24" />
        <path d="M10 10 L10 4 M10 4 Q 13 2 16 4" />
        <path d="M22 10 L22 4 M22 4 Q 19 2 16 4" />
        <path d="M13 26 L19 26 M16 24 L16 28" />
      </g>
    ),
    lotus: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M16 22 Q 8 18 8 10 Q 12 12 16 22" />
        <path d="M16 22 Q 24 18 24 10 Q 20 12 16 22" />
        <path d="M16 22 Q 16 10 16 6" />
        <path d="M6 22 Q 16 26 26 22" />
      </g>
    ),
    conch: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M22 8 Q 28 14 24 22 Q 18 28 10 24 Q 4 18 8 12 Q 14 8 18 12 Q 20 16 16 18" />
      </g>
    ),
    flame: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M16 26 Q 8 22 10 14 Q 12 18 14 16 Q 12 10 16 6 Q 16 12 18 12 Q 20 10 20 14 Q 24 18 22 22 Q 20 26 16 26 Z" />
      </g>
    ),
    arrow: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M6 26 L26 6" />
        <path d="M20 6 L26 6 L26 12" />
        <path d="M6 22 L10 26" />
        <path d="M8 20 L4 24" />
      </g>
    ),
    gem: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M16 4 L26 14 L16 28 L6 14 Z" />
        <path d="M6 14 L26 14 M16 4 L12 14 M16 4 L20 14 M12 14 L16 28 M20 14 L16 28" opacity="0.6" />
      </g>
    ),
    scroll: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M7 8 Q 7 6 9 6 L 23 6 Q 25 6 25 8 L 25 22 Q 25 26 22 26 L 10 26 Q 7 26 7 22 Z" />
        <path d="M11 12 L 21 12 M 11 16 L 21 16 M 11 20 L 18 20" opacity="0.6" />
      </g>
    ),
    shield: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op}>
        <path d="M16 4 L26 8 L26 16 Q 26 24 16 28 Q 6 24 6 16 L 6 8 Z" />
        <circle cx="16" cy="16" r="3" />
      </g>
    ),
  };
  return (
    <svg width={size} height={size} viewBox="0 0 32 32" style={{ display: 'block' }}>
      {s[type] || s.chakra}
    </svg>
  );
}

// Portrait placeholder — parchment-textured bust area with monospace label
function PortraitPlaceholder({ width = 280, height = 360, name, epithet, tone = 'indigo' }) {
  const tones = {
    indigo: { bg1: '#2a2560', bg2: '#1a1540', stripe: 'rgba(232, 198, 106, 0.08)', border: '#c9993a', text: 'rgba(240,229,200,0.55)' },
    brass:  { bg1: '#3d2f18', bg2: '#231a0c', stripe: 'rgba(232, 198, 106, 0.10)', border: '#e8c66a', text: 'rgba(240,229,200,0.6)' },
    parch:  { bg1: '#d9c99a', bg2: '#b8a572', stripe: 'rgba(15, 11, 46, 0.10)', border: '#1a1540', text: 'rgba(30,20,60,0.55)' },
    wine:   { bg1: '#4a1a2a', bg2: '#2a0c18', stripe: 'rgba(232, 198, 106, 0.10)', border: '#c9993a', text: 'rgba(240,229,200,0.55)' },
  };
  const t = tones[tone] || tones.indigo;
  return (
    <div style={{
      width, height, position: 'relative', overflow: 'hidden',
      background: `linear-gradient(160deg, ${t.bg1}, ${t.bg2})`,
      border: `1.5px solid ${t.border}`,
    }}>
      {/* diagonal stripe texture as stand-in for illustrative bust */}
      <div style={{
        position: 'absolute', inset: 0,
        backgroundImage: `repeating-linear-gradient(135deg, transparent 0 8px, ${t.stripe} 8px 9px)`,
      }} />
      {/* subtle vignette */}
      <div style={{
        position: 'absolute', inset: 0,
        background: 'radial-gradient(ellipse at 50% 35%, transparent 40%, rgba(0,0,0,0.35) 100%)',
      }} />
      {/* monospace placeholder label */}
      <div style={{
        position: 'absolute', bottom: 14, left: 14, right: 14,
        fontFamily: 'JetBrains Mono, monospace', fontSize: 10,
        color: t.text, letterSpacing: 0.5,
        display: 'flex', justifyContent: 'space-between',
      }}>
        <span>[ portrait: {name} ]</span>
        <span>{width}×{height}</span>
      </div>
      {epithet && (
        <div style={{
          position: 'absolute', top: 14, left: 14,
          fontFamily: 'JetBrains Mono, monospace', fontSize: 10,
          color: t.text, letterSpacing: 0.5,
        }}>{epithet}</div>
      )}
    </div>
  );
}

Object.assign(window, {
  FiligreeCorner, LotusMedallion, PaisleyBorder, OgeeArch, ItemSigil, PortraitPlaceholder,
});
