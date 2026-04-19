// App — composes the three variants on the design canvas.

function App() {
  return (
    <DesignCanvas>
      <div style={{
        padding: '0 60px 40px',
        fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", system-ui, sans-serif',
      }}>
        <div style={{
          fontFamily: 'Cormorant Garamond, serif',
          fontSize: 48, fontWeight: 600, fontStyle: 'italic',
          color: '#2b1810', letterSpacing: -0.5, marginBottom: 4,
        }}>Project Astra — Trade Screen</div>
        <div style={{
          fontSize: 15, color: 'rgba(60,50,40,0.65)', maxWidth: 760,
          lineHeight: 1.55, marginBottom: 6,
        }}>
          Three full-screen mockups at 1920×1080. Each preserves the reference's two-unit facing
          layout — portrait + 5-row inventory per unit — but reinterprets it through an Indian-mythological,
          painterly aesthetic: temple brass, indigo manuscript, gold filigree, serif typography.
          All six interactive states (default · hover · pressed · focused · selected · disabled) are
          visible within each mockup; state labels are overlaid in small mono caps for review.
        </div>
        <div style={{
          fontFamily: 'JetBrains Mono, monospace', fontSize: 11,
          color: 'rgba(60,50,40,0.5)', letterSpacing: 1,
        }}>portraits & item art are placeholders — swap in illustrated bust art and painted icons once commissioned</div>
      </div>

      <DCSection
        title="Variant 1 — Indigo Codex"
        subtitle="Illuminated manuscript. Two facing pages bound by a brass spine. Reads left-to-right like the reference; cleanest migration from GBA layout."
      >
        <DCArtboard label="1920 × 1080  ·  full screen" width={1920} height={1080}>
          <VariantCodex />
        </DCArtboard>
        <DCPostIt top={40} left={-40} rotate={-3}>
          <strong>States on-screen:</strong><br/>
          Left rows: default, hover, selected, disabled, empty<br/>
          Right rows: focused, pressed + defaults<br/>
          Action bar cycles all 5 button states
        </DCPostIt>
      </DCSection>

      <DCSection
        title="Variant 2 — Temple Brass"
        subtitle="Portraits as oval cameos under ogee arches. Inventories engraved on parchment plaques. Cast-metal action buttons with raised press physics."
      >
        <DCArtboard label="1920 × 1080  ·  full screen" width={1920} height={1080}>
          <VariantBrass />
        </DCArtboard>
        <DCPostIt top={40} left={-40} rotate={2}>
          <strong>Heavier chrome than v1.</strong><br/>
          Good if the game wants a "grand ritual" moment for trades.
          Parchment panels read well at distance.
        </DCPostIt>
      </DCSection>

      <DCSection
        title="Variant 3 — Mandala Sandhi"
        subtitle="Breaks the two-column grid. Circular portraits with petal halos; inventory rows curve around each sage; center holds the item being exchanged. More experimental, more ceremonial."
      >
        <DCArtboard label="1920 × 1080  ·  full screen" width={1920} height={1080}>
          <VariantMandala />
        </DCArtboard>
        <DCPostIt top={40} left={-40} rotate={-2}>
          <strong>Most divergent.</strong><br/>
          Central "offering bowl" visualises the held item between companions —
          gives the exchange a narrative beat instead of a silent swap.
        </DCPostIt>
      </DCSection>

      <div style={{ padding: '0 60px 80px', maxWidth: 900 }}>
        <div style={{
          fontFamily: 'Cormorant Garamond, serif',
          fontSize: 24, fontWeight: 600, color: '#2b1810',
          marginBottom: 8,
        }}>Notes & next steps</div>
        <ul style={{
          fontSize: 14, color: 'rgba(60,50,40,0.75)', lineHeight: 1.7,
          paddingLeft: 20, margin: 0,
        }}>
          <li>All item sigils are original abstract brass-line marks (chakra, trishul-haft, lotus, conch, flame, arrow, gem, scroll, shield) — not illustrations of real weapons. Replace with painted icons when art is ready.</li>
          <li>Portraits are textured parchment placeholders with monospace labels; the layouts assume painted busts will land in those slots.</li>
          <li>Ornament (filigree corners, paisley borders, lotus medallions, ogee arches, 16-petal mandala) is all generated geometry — no traced copyrighted ornament.</li>
          <li>Each variant carries its own type hierarchy using Cinzel + Cormorant Garamond + EB Garamond + JetBrains Mono for diegetic labels.</li>
          <li>Once you pick a direction I can: wire this into a real clickable prototype (keyboard/controller nav, animated swap), add a confirm-modal for legendary items, and build the controller-prompt variant.</li>
        </ul>
      </div>
    </DesignCanvas>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
