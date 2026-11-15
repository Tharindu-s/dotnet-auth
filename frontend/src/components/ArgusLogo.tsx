import { Link } from 'react-router'
import { cn } from '@/lib/utils'

// Vector version of the Argus mark from public/Logos. A 3x3 grid of dots on an 89-unit spacing:
// small corners, medium edges, and a large blue center.
//   standard: for 48px and up
//   small:    heavier dots, for 32px and below (as in "Final — small sizes")
const geometry = {
  standard: { corner: 16.5, edge: 23, center: 39 },
  small: { corner: 18.7, edge: 23.4, center: 42 },
} as const

const centers = [23, 112, 201]

interface ArgusMarkProps {
  size?: number
  cut?: keyof typeof geometry
  /** Use on dark backgrounds: light dots and the lighter blue */
  onDark?: boolean
  className?: string
}

export function ArgusMark({ size = 32, cut = size <= 32 ? 'small' : 'standard', onDark = false, className }: ArgusMarkProps) {
  const r = geometry[cut]
  const dots = onDark ? '#f5f6f8' : '#0e1116'
  const blue = onDark ? '#6e8bff' : '#2f5bff'

  return (
    <svg
      width={size}
      height={size}
      viewBox="-20 -20 264 264"
      aria-hidden
      className={cn('shrink-0', className)}
    >
      {centers.flatMap((cy, row) =>
        centers.map((cx, col) => {
          const isCenter = row === 1 && col === 1
          const isEdge = row === 1 || col === 1
          return (
            <circle
              key={`${row}-${col}`}
              cx={cx}
              cy={cy}
              r={isCenter ? r.center : isEdge ? r.edge : r.corner}
              fill={isCenter ? blue : dots}
            />
          )
        }),
      )}
    </svg>
  )
}

/** Mark plus "argus", linking home. Proportions follow the lockup in public/Logos. */
export function ArgusLogo({ className }: { className?: string }) {
  return (
    <Link to="/" aria-label="Argus home" className={cn('flex items-center gap-2.5 text-[#0e1116]', className)}>
      <ArgusMark size={26} />
      <span className="text-[1.375rem] leading-none font-medium tracking-[-0.03em]">argus</span>
    </Link>
  )
}

/** The mark on a soft peach and lilac glow, above welcome headings */
export function ArgusGlowMark({ size = 56, className }: { size?: number; className?: string }) {
  return (
    <div aria-hidden className={cn('relative grid place-items-center', className)} style={{ width: size * 2.4, height: size * 2.4 }}>
      <div className="mark-glow absolute inset-0 rounded-full" />
      <ArgusMark size={size} cut="standard" className="relative drop-shadow-sm" />
    </div>
  )
}
