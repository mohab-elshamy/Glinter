import * as DialogPrimitive from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import type { ReactNode } from "react";
import { overlayLayers } from "@/shared/lib/overlay-layers";

export const Dialog = ({
  open,
  onOpenChange,
  title,
  description,
  children,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  children: ReactNode;
}) => (
  <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className={`${overlayLayers.modalBackdrop} fixed inset-0 bg-black/60 backdrop-blur-sm`} />
      <DialogPrimitive.Content aria-modal="true" className={`${overlayLayers.modal} fixed left-1/2 top-1/2 max-h-[90dvh] w-[calc(100%-2rem)] max-w-lg -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-2xl border border-border bg-background p-5 shadow-2xl focus:outline-none`}>
        <div className="pr-10">
          <DialogPrimitive.Title className="text-lg font-bold">{title}</DialogPrimitive.Title>
          {description && <DialogPrimitive.Description className="mt-1 text-sm text-muted-foreground">{description}</DialogPrimitive.Description>}
        </div>
        <DialogPrimitive.Close className="absolute right-4 top-4 rounded-lg border border-border p-2" aria-label="Close dialog"><X className="h-4 w-4" /></DialogPrimitive.Close>
        <div className="mt-4">{children}</div>
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  </DialogPrimitive.Root>
);
