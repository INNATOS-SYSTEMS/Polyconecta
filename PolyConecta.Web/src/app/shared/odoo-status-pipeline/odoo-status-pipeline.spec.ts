import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { OdooStatusPipeline } from './odoo-status-pipeline';

describe('OdooStatusPipeline', () => {
  it('marca las etapas anteriores como hechas y la actual como activa', () => {
    const fixture = TestBed.createComponent(OdooStatusPipeline);
    fixture.componentRef.setInput('stages', ['Borrador', 'Confirmado', 'Autorizado', 'Hecho']);
    fixture.componentRef.setInput('currentStage', 'Autorizado');
    fixture.detectChanges();

    const pasos = [...fixture.nativeElement.querySelectorAll('.arrow-step')] as HTMLElement[];
    expect(pasos.map(p => p.className.replace('arrow-step', '').trim())).toEqual(['done', 'done', 'active', '']);
  });
});
