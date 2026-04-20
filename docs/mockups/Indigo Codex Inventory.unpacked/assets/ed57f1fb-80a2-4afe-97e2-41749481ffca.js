// Shared item-type sigils for Project Astra inventory popup.
// All original, abstract, dharmic — not reused from FE or any other IP.
// Each is a small brass-line glyph, 32x32 viewBox, stroke-based so color/weight
// can be reskinned per variant.

function AstraSigil({ type, size = 28, color = '#e8c66a', dim = false, strokeWidth = 1.5 }) {
  const op = dim ? 0.35 : 1;
  const sw = strokeWidth;
  const glyphs = {
    // sword — khadga: straight blade, crossguard, diamond pommel
    sword: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op} strokeLinecap="round" strokeLinejoin="round">
        <path d="M16 3 L 16 21" />
        <path d="M13 5 L 16 3 L 19 5" />
        <path d="M10 21 L 22 21" />
        <path d="M16 21 L 16 25" />
        <path d="M13 25 L 16 28 L 19 25 L 16 22 Z" fill={color} opacity={op * 0.9} />
      </g>
    ),
    // lance — shula: tri-point spear head on long haft
    lance: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op} strokeLinecap="round" strokeLinejoin="round">
        <path d="M16 2 L 13 10 L 16 8 L 19 10 Z" fill={color} opacity={op * 0.9} />
        <path d="M16 10 L 16 26" />
        <path d="M12 6 L 14 9" />
        <path d="M20 6 L 18 9" />
        <path d="M13 28 L 19 28" />
      </g>
    ),
    // axe — kuthara: crescent blade on haft
    axe: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op} strokeLinecap="round" strokeLinejoin="round">
        <path d="M16 4 L 16 28" />
        <path d="M16 8 Q 26 10 24 18 Q 22 16 16 16" fill={color} opacity={op * 0.75} />
        <path d="M16 8 Q 6 10 8 18 Q 10 16 16 16" fill={color} opacity={op * 0.75} />
        <circle cx="16" cy="28" r="1.4" fill={color} opacity={op} />
      </g>
    ),
    // bow — dhanush: curved arc with arrow and string
    bow: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op} strokeLinecap="round" strokeLinejoin="round">
        <path d="M10 4 Q 24 16 10 28" />
        <path d="M10 4 L 10 28" strokeDasharray="1.5 2" />
        <path d="M4 16 L 22 16" />
        <path d="M18 13 L 22 16 L 18 19" />
        <path d="M4 14 L 6 16 L 4 18" />
      </g>
    ),
    // staff — danda: straight staff with lotus bud top
    staff: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op} strokeLinecap="round" strokeLinejoin="round">
        <path d="M16 10 L 16 28" />
        <circle cx="16" cy="7" r="3" />
        <path d="M14 5 Q 16 2 18 5" />
        <path d="M12 8 Q 16 10 20 8" />
        <path d="M14 28 L 18 28" />
      </g>
    ),
    // consumable — kalash: small pot with flame/drop above
    consumable: (
      <g stroke={color} strokeWidth={sw} fill="none" opacity={op} strokeLinecap="round" strokeLinejoin="round">
        <path d="M10 14 Q 10 22 12 26 L 20 26 Q 22 22 22 14 Z" />
        <path d="M9 14 L 23 14" />
        <path d="M12 14 L 12 11 L 20 11 L 20 14" />
        <path d="M16 8 Q 14 6 16 3 Q 18 6 16 8 Z" fill={color} opacity={op * 0.9} />
      </g>
    ),
  };
  return (
    <svg width={size} height={size} viewBox="0 0 32 32" style={{ display: 'block' }}>
      {glyphs[type] || glyphs.sword}
    </svg>
  );
}

// Small kirtimukha (sacred face) medallion — used as a heraldic seal
function Kirtimukha({ size = 48, color = '#e8c66a', bg = 'transparent' }) {
  return (
    <svg width={size} height={size} viewBox="0 0 48 48" style={{ display: 'block' }}>
      {bg !== 'transparent' && <circle cx="24" cy="24" r="22" fill={bg} />}
      <g stroke={color} strokeWidth="1.3" fill="none" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="24" cy="24" r="20" opacity="0.6" />
        <circle cx="24" cy="24" r="15" />
        {/* stylized face: eyes + fangs + crown tuft */}
        <path d="M24 10 Q 20 13 18 10 M24 10 Q 28 13 30 10" />
        <circle cx="19" cy="22" r="1.5" fill={color} />
        <circle cx="29" cy="22" r="1.5" fill={color} />
        <path d="M17 28 Q 20 34 24 30 Q 28 34 31 28" />
        <path d="M20 30 L 20 33" />
        <path d="M28 30 L 28 33" />
        <path d="M24 24 L 24 30" opacity="0.7" />
      </g>
    </svg>
  );
}

// A simple paisley / buti repeating band
function ButiBand({ width = 400, height = 18, color = '#c9993a', opacity = 0.9 }) {
  const n = Math.floor(width / 36);
  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`} style={{ display: 'block' }}>
      <line x1="0" y1={height/2} x2={width} y2={height/2} stroke={color} strokeWidth="0.6" opacity="0.4" />
      {[...Array(n)].map((_, i) => {
        const x = 18 + i * 36;
        return (
          <g key={i} stroke={color} fill="none" strokeWidth="1" opacity={opacity}>
            <path d={`M ${x-6} ${height/2} Q ${x-7} ${height/2-5} ${x} ${height/2-5} Q ${x+8} ${height/2-4} ${x+5} ${height/2+2} Q ${x+2} ${height/2+5} ${x-2} ${height/2+2} Q ${x-4} ${height/2} ${x-1} ${height/2-1}`} />
            <circle cx={x} cy={height/2} r="0.9" fill={color} />
          </g>
        );
      })}
    </svg>
  );
}

// Corner filigree — 4-rotatable
function CornerFiligree({ size = 44, color = '#c9993a', rotation = 0, opacity = 1 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 48 48"
      style={{ transform: `rotate(${rotation}deg)`, opacity, display: 'block' }}>
      <g fill="none" stroke={color} strokeWidth="1.1" strokeLinecap="round">
        <path d="M2 2 L 10 2 L 10 6 Q 10 10 14 10 Q 18 10 18 14 Q 18 20 24 20" />
        <path d="M2 10 Q 6 10 6 14" opacity="0.7" />
        <circle cx="24" cy="20" r="1.5" fill={color} stroke="none" />
        <path d="M2 18 Q 6 18 8 20" opacity="0.5" />
        <path d="M14 6 L 18 6" opacity="0.5" />
      </g>
    </svg>
  );
}

Object.assign(window, { AstraSigil, Kirtimukha, ButiBand, CornerFiligree });
