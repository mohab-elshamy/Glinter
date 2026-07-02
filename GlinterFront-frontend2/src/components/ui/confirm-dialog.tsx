import * as AlertDialog from "@radix-ui/react-alert-dialog";
import type { ReactNode } from "react";
import { overlayLayers } from "@/shared/lib/overlay-layers";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description: string;
  confirmLabel?: string;
  destructive?: boolean;
  onConfirm: () => void;
  onOpenChange: (open: boolean) => void;
  children?: ReactNode;
}

export const ConfirmDialog = ({
  open,
  title,
  description,
  confirmLabel = "Confirm",
  destructive,
  onConfirm,
  onOpenChange,
  children,
}: ConfirmDialogProps) => (
  <AlertDialog.Root open={open} onOpenChange={onOpenChange}>
    {children && <AlertDialog.Trigger asChild>{children}</AlertDialog.Trigger>}
    <AlertDialog.Portal>
      <AlertDialog.Overlay className={`${overlayLayers.modalBackdrop} fixed inset-0 bg-black/70 backdrop-blur-sm`} />
      <AlertDialog.Content className={`${overlayLayers.modalContent} fixed left-1/2 top-1/2 max-h-[min(90vh,32rem)] w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-2xl border border-border bg-card p-5 shadow-2xl focus:outline-none sm:p-6`}>
        <AlertDialog.Title className="text-lg font-bold">{title}</AlertDialog.Title>
        <AlertDialog.Description className="mt-2 text-sm leading-6 text-muted-foreground">
          {description}
        </AlertDialog.Description>
        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <AlertDialog.Cancel className="rounded-lg border border-border px-4 py-2.5 text-sm">
            Cancel
          </AlertDialog.Cancel>
          <AlertDialog.Action
            onClick={onConfirm}
            className={`rounded-lg px-4 py-2.5 text-sm font-semibold ${
              destructive ? "bg-destructive text-destructive-foreground" : "bg-primary text-primary-foreground"
            }`}
          >
            {confirmLabel}
          </AlertDialog.Action>
        </div>
      </AlertDialog.Content>
    </AlertDialog.Portal>
  </AlertDialog.Root>
);
