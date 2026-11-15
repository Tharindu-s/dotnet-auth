import type { CSSProperties, ReactNode } from 'react'
import { ArgusLogo } from '@/components/ArgusLogo'
import { cn } from '@/lib/utils'

const violet = '#934bf0'
const pink = '#fa7fd8'
const coral = '#fa7465'
const lilac = '#c7a6f8'
const blush = '#f7b3a8'

interface Block {
  width: string
  height: string
  background: string
}

// Stacks of grainy color blocks hugging the left and right edges, like the reference hero.
// Widths are relative to the column, heights to the full height.
const leftBlocks: Block[] = [
  { width: '100%', height: '17%', background: `linear-gradient(115deg, #c45ff0, ${violet})` },
  { width: '62%', height: '17%', background: `linear-gradient(90deg, #a457d8, ${coral})` },
  { width: '19%', height: '6%', background: coral },
  { width: '33%', height: '10%', background: violet },
  { width: '23%', height: '3.5%', background: `linear-gradient(90deg, ${blush}, ${coral})` },
  { width: '48%', height: '20%', background: `linear-gradient(160deg, ${lilac}, #a77cf2)` },
  { width: '73%', height: '7.5%', background: `linear-gradient(90deg, ${blush}, ${coral})` },
  { width: '100%', height: '19%', background: `linear-gradient(110deg, ${violet}, ${pink} 55%, ${coral})` },
]

const rightBlocks: Block[] = [
  { width: '100%', height: '17%', background: `linear-gradient(100deg, ${coral}, ${pink} 55%, #b65cf0)` },
  { width: '74%', height: '7.5%', background: `linear-gradient(90deg, ${coral}, #fb8a74)` },
  { width: '48%', height: '20%', background: violet },
  { width: '23%', height: '3.5%', background: coral },
  { width: '33%', height: '10.5%', background: `linear-gradient(135deg, #a45cf3, ${violet})` },
  { width: '19%', height: '6.5%', background: coral },
  { width: '62%', height: '17.5%', background: `linear-gradient(200deg, ${blush}, #c87a9e 60%, #9b5fd6)` },
  { width: '100%', height: '17.5%', background: `linear-gradient(110deg, #b48af5, ${pink})` },
]

function BlockColumn({ blocks, side }: { blocks: Block[]; side: 'left' | 'right' }) {
  return (
    <div
      className={cn('flex h-full w-[22%] max-w-[300px] flex-col', side === 'left' ? 'items-start' : 'items-end')}
    >
      {blocks.map((block, i) => (
        <div key={i} className="grain shrink-0" style={block as CSSProperties} />
      ))}
    </div>
  )
}

/** Decorative frame for full-page layouts. Hidden on small screens, where it would crowd the content. */
export function GradientBlocks({ className }: { className?: string }) {
  return (
    <div aria-hidden className={cn('pointer-events-none absolute inset-0 hidden justify-between lg:flex', className)}>
      <BlockColumn blocks={leftBlocks} side="left" />
      <BlockColumn blocks={rightBlocks} side="right" />
    </div>
  )
}

/** A small row of blocks used as an accent next to page titles */
export function BlockAccent({ className }: { className?: string }) {
  return (
    <div aria-hidden className={cn('flex h-16 items-end gap-0', className)}>
      <div className="grain h-full w-16" style={{ background: `linear-gradient(160deg, ${pink}, ${violet})` }} />
      <div className="grain h-2/3 w-10" style={{ background: coral }} />
      <div className="grain h-1/3 w-6" style={{ background: violet }} />
      <div className="grain h-1/2 w-12" style={{ background: `linear-gradient(90deg, ${blush}, ${coral})` }} />
    </div>
  )
}

/** Pill label with a dot, used above headlines */
export function Eyebrow({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-2 rounded-full bg-surface px-3 py-1 text-[0.8125rem] font-medium text-ink',
        className,
      )}
    >
      <span className="size-1.5 rounded-full bg-argus" aria-hidden />
      {children}
    </span>
  )
}

/** White top bar: wordmark on the left, optional nav in the middle, actions on the right */
export function SiteHeader({ nav, actions }: { nav?: ReactNode; actions?: ReactNode }) {
  return (
    <header className="relative z-20 bg-background">
      {/* Full width, with the same side inset as the color blocks so the edges line up */}
      <div className="flex h-16 items-center gap-4 px-4 sm:px-6 md:grid md:grid-cols-[1fr_auto_1fr] lg:px-10">
        <ArgusLogo />
        <div className="hidden justify-self-center md:block">{nav}</div>
        <div className="ml-auto flex items-center justify-self-end gap-3 md:ml-0">{actions}</div>
      </div>
      {/* On phones the nav gets its own row */}
      {nav && <div className="overflow-x-auto border-y bg-background px-4 py-2.5 sm:px-6 md:hidden">{nav}</div>}
    </header>
  )
}
