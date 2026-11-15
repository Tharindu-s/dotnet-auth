import { Check, Copy } from 'lucide-react'
import { useId, useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Field, FieldDescription, FieldLabel } from '@/components/ui/field'
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from '@/components/ui/input-group'

function useCopy(label: string) {
  const [copied, setCopied] = useState(false)
  const copy = async (value: string) => {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
      toast.success(`${label} copied.`)
    } catch {
      toast.error('Could not copy. Select the text and copy it yourself.')
    }
  }
  return { copied, copy }
}

/** A read-only value with a copy button (shadcn Field + InputGroup) */
export function CopyInput({ label, value, description }: { label: string; value: string; description?: string }) {
  const id = useId()
  const { copied, copy } = useCopy(label)

  return (
    <Field>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <InputGroup className="h-10 bg-white">
        <InputGroupInput id={id} value={value} readOnly className="font-mono text-xs" onFocus={(e) => e.currentTarget.select()} />
        <InputGroupAddon align="inline-end">
          <InputGroupButton size="icon-xs" onClick={() => copy(value)} aria-label={`Copy ${label}`}>
            {copied ? <Check /> : <Copy />}
          </InputGroupButton>
        </InputGroupAddon>
      </InputGroup>
      {description && <FieldDescription>{description}</FieldDescription>}
    </Field>
  )
}

/** A copyable code sample */
export function CodeBlock({ label, code }: { label: string; code: string }) {
  const { copied, copy } = useCopy(label)

  return (
    <div className="grid gap-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-sm font-medium text-ink">{label}</span>
        <Button type="button" variant="ghost" size="sm" onClick={() => copy(code)}>
          {copied ? <Check /> : <Copy />} Copy
        </Button>
      </div>
      <pre className="overflow-x-auto rounded-2xl bg-ink p-4 text-xs leading-relaxed text-white/90">
        <code>{code}</code>
      </pre>
    </div>
  )
}
