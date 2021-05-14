﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace DICOMAnonymizer.Tools
{
    using System.Collections.Generic;

    public enum SOPClass
    {
        ComputedRadiographyImageStorage,
        DigitalXRayImageStorageForPresentation,
        DigitalXRayImageStorageForProcessing,
        DigitalMammographyXRayImageStorageForPresentation,
        DigitalMammographyXRayImageStorageForProcessing,
        DigitalIntraOralXRayImageStorageForPresentation,
        DigitalIntraOralXRayImageStorageForProcessing,
        CTImageStorage,
        EnhancedCTImageStorage,
        LegacyConvertedEnhancedCTImageStorage,
        UltrasoundMultiframeImageStorage,
        MRImageStorage,
        EnhancedMRImageStorage,
        MRSpectroscopyStorage,
        EnhancedMRColorImageStorage,
        LegacyConvertedEnhancedMRImageStorage,
        UltrasoundImageStorage,
        EnhancedUSVolumeStorage,
        SecondaryCaptureImageStorage,
        MultiframeSingleBitSecondaryCaptureImageStorage,
        MultiframeGrayscaleByteSecondaryCaptureImageStorage,
        MultiframeGrayscaleWordSecondaryCaptureImageStorage,
        MultiframeTrueColorSecondaryCaptureImageStorage,
#pragma warning disable CA1707 // Identifiers should not contain underscores
        _12leadECGWaveformStorage,
#pragma warning restore CA1707 // Identifiers should not contain underscores
        GeneralECGWaveformStorage,
        AmbulatoryECGWaveformStorage,
        HemodynamicWaveformStorage,
        CardiacElectrophysiologyWaveformStorage,
        BasicVoiceAudioWaveformStorage,
        GeneralAudioWaveformStorage,
        ArterialPulseWaveformStorage,
        RespiratoryWaveformStorage,
        GrayscaleSoftcopyPresentationStateStorage,
        ColorSoftcopyPresentationStateStorage,
        PseudoColorSoftcopyPresentationStateStorage,
        BlendingSoftcopyPresentationStateStorage,
        XAXRFGrayscaleSoftcopyPresentationStateStorage,
        GrayscalePlanarMPRVolumetricPresentationStateStorage,
        CompositingPlanarMPRVolumetricPresentationStateStorage,
        AdvancedBlendingPresentationStateStorage,
        VolumeRenderingVolumetricPresentationStateStorage,
        SegmentedVolumeRenderingVolumetricPresentationStateStorage,
        MultipleVolumeRenderingVolumetricPresentationStateStorage,
        XRayAngiographicImageStorage,
        EnhancedXAImageStorage,
        XRayRadiofluoroscopicImageStorage,
        EnhancedXRFImageStorage,
        XRay3DAngiographicImageStorage,
        XRay3DCraniofacialImageStorage,
        BreastTomosynthesisImageStorage,
        BreastProjectionXRayImageStorageForPresentation,
        BreastProjectionXRayImageStorageForProcessing,
        IntravascularOpticalCoherenceTomographyImageStorageForPresentation,
        IntravascularOpticalCoherenceTomographyImageStorageForProcessing,
        NuclearMedicineImageStorage,
        ParametricMapStorage,
        RawDataStorage,
        SpatialRegistrationStorage,
        SpatialFiducialsStorage,
        DeformableSpatialRegistrationStorage,
        SegmentationStorage,
        SurfaceSegmentationStorage,
        TractographyResultsStorage,
        RealWorldValueMappingStorage,
        SurfaceScanMeshStorage,
        SurfaceScanPointCloudStorage,
        VLEndoscopicImageStorage,
        VideoEndoscopicImageStorage,
        VLMicroscopicImageStorage,
        VideoMicroscopicImageStorage,
        VLSlideCoordinatesMicroscopicImageStorage,
        VLPhotographicImageStorage,
        VideoPhotographicImageStorage,
        OphthalmicPhotography8BitImageStorage,
        OphthalmicPhotography16BitImageStorage,
        StereometricRelationshipStorage,
        OphthalmicTomographyImageStorage,
        WideFieldOphthalmicPhotographyStereographicProjectionImageStorage,
        WideFieldOphthalmicPhotography3DCoordinatesImageStorage,
        OphthalmicOpticalCoherenceTomographyEnFaceImageStorage,
        OphthalmicOpticalCoherenceTomographyBscanVolumeAnalysisStorage,
        VLWholeSlideMicroscopyImageStorage,
        LensometryMeasurementsStorage,
        AutorefractionMeasurementsStorage,
        KeratometryMeasurementsStorage,
        SubjectiveRefractionMeasurementsStorage,
        VisualAcuityMeasurementsStorage,
        SpectaclePrescriptionReportStorage,
        OphthalmicAxialMeasurementsStorage,
        IntraocularLensCalculationsStorage,
        MacularGridThicknessandVolumeReport,
        OphthalmicVisualFieldStaticPerimetryMeasurementsStorage,
        OphthalmicThicknessMapStorage,
        CornealTopographyMapStorage,
        BasicTextSRStorage,
        EnhancedSRStorage,
        ComprehensiveSRStorage,
        Comprehensive3DSRStorage,
        ExtensibleSRStorage,
        ProcedureLogStorage,
        MammographyCADSRStorage,
        KeyObjectSelectionStorage,
        ChestCADSRStorage,
        XRayRadiationDoseSRStorage,
        RadiopharmaceuticalRadiationDoseSRStorage,
        ColonCADSRStorage,
        ImplantationPlanSRDocumentStorage,
        AcquisitionContextSRStorage,
        SimplifiedAdultEchoSRStorage,
        PatientRadiationDoseSRStorage,
        ContentAssessmentResultsStorage,
        EncapsulatedPDFStorage,
        EncapsulatedCDAStorage,
        PositronEmissionTomographyImageStorage,
        EnhancedPETImageStorage,
        LegacyConvertedEnhancedPETImageStorage,
        BasicStructuredDisplayStorage,
        CTPerformedProcedureProtocolStorage,
        RTImageStorage,
        RTDoseStorage,
        RTStructureSetStorage,
        RTBeamsTreatmentRecordStorage,
        RTPlanStorage,
        RTBrachyTreatmentRecordStorage,
        RTTreatmentSummaryRecordStorage,
        RTIonPlanStorage,
        RTIonBeamsTreatmentRecordStorage,
        RTBeamsDeliveryInstructionStorage,
        RTBrachyApplicationSetupDeliveryInstructionStorage,
    }

    public static class SOPClassFinder
    {

        #region SOP Class Dictionaries

        private static readonly Dictionary<SOPClass, string> _sopNameMap = new Dictionary<SOPClass, string>()
        {
            {SOPClass.ComputedRadiographyImageStorage, "1.2.840.10008.5.1.4.1.1.1"},
            {SOPClass.DigitalXRayImageStorageForPresentation, "1.2.840.10008.5.1.4.1.1.1.1"},
            {SOPClass.DigitalXRayImageStorageForProcessing, "1.2.840.10008.5.1.4.1.1.1.1.1"},
            {SOPClass.DigitalMammographyXRayImageStorageForPresentation, "1.2.840.10008.5.1.4.1.1.1.2"},
            {SOPClass.DigitalMammographyXRayImageStorageForProcessing, "1.2.840.10008.5.1.4.1.1.1.2.1"},
            {SOPClass.DigitalIntraOralXRayImageStorageForPresentation, "1.2.840.10008.5.1.4.1.1.1.3"},
            {SOPClass.DigitalIntraOralXRayImageStorageForProcessing, "1.2.840.10008.5.1.4.1.1.1.3.1"},
            {SOPClass.CTImageStorage, "1.2.840.10008.5.1.4.1.1.2"},
            {SOPClass.EnhancedCTImageStorage, "1.2.840.10008.5.1.4.1.1.2.1"},
            {SOPClass.LegacyConvertedEnhancedCTImageStorage, "1.2.840.10008.5.1.4.1.1.2.2"},
            {SOPClass.UltrasoundMultiframeImageStorage, "1.2.840.10008.5.1.4.1.1.3.1"},
            {SOPClass.MRImageStorage, "1.2.840.10008.5.1.4.1.1.4"},
            {SOPClass.EnhancedMRImageStorage, "1.2.840.10008.5.1.4.1.1.4.1"},
            {SOPClass.MRSpectroscopyStorage, "1.2.840.10008.5.1.4.1.1.4.2"},
            {SOPClass.EnhancedMRColorImageStorage, "1.2.840.10008.5.1.4.1.1.4.3"},
            {SOPClass.LegacyConvertedEnhancedMRImageStorage, "1.2.840.10008.5.1.4.1.1.4.4"},
            {SOPClass.UltrasoundImageStorage, "1.2.840.10008.5.1.4.1.1.6.1"},
            {SOPClass.EnhancedUSVolumeStorage, "1.2.840.10008.5.1.4.1.1.6.2"},
            {SOPClass.SecondaryCaptureImageStorage, "1.2.840.10008.5.1.4.1.1.7"},
            {SOPClass.MultiframeSingleBitSecondaryCaptureImageStorage, "1.2.840.10008.5.1.4.1.1.7.1"},
            {SOPClass.MultiframeGrayscaleByteSecondaryCaptureImageStorage, "1.2.840.10008.5.1.4.1.1.7.2"},
            {SOPClass.MultiframeGrayscaleWordSecondaryCaptureImageStorage, "1.2.840.10008.5.1.4.1.1.7.3"},
            {SOPClass.MultiframeTrueColorSecondaryCaptureImageStorage, "1.2.840.10008.5.1.4.1.1.7.4"},
            {SOPClass._12leadECGWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.1.1"},
            {SOPClass.GeneralECGWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.1.2"},
            {SOPClass.AmbulatoryECGWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.1.3"},
            {SOPClass.HemodynamicWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.2.1"},
            {SOPClass.CardiacElectrophysiologyWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.3.1"},
            {SOPClass.BasicVoiceAudioWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.4.1"},
            {SOPClass.GeneralAudioWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.4.2"},
            {SOPClass.ArterialPulseWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.5.1"},
            {SOPClass.RespiratoryWaveformStorage, "1.2.840.10008.5.1.4.1.1.9.6.1"},
            {SOPClass.GrayscaleSoftcopyPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.1"},
            {SOPClass.ColorSoftcopyPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.2"},
            {SOPClass.PseudoColorSoftcopyPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.3"},
            {SOPClass.BlendingSoftcopyPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.4"},
            {SOPClass.XAXRFGrayscaleSoftcopyPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.5"},
            {SOPClass.GrayscalePlanarMPRVolumetricPresentationStateStorage, "1.2.840.10008.​5.​1.​4.​1.​1.​11.​6"},
            {SOPClass.CompositingPlanarMPRVolumetricPresentationStateStorage, "1.2.840.10008.​5.​1.​4.​1.​1.​11.​7"},
            {SOPClass.AdvancedBlendingPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.8"},
            {SOPClass.VolumeRenderingVolumetricPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.9"},
            {SOPClass.SegmentedVolumeRenderingVolumetricPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.10"},
            {SOPClass.MultipleVolumeRenderingVolumetricPresentationStateStorage, "1.2.840.10008.5.1.4.1.1.11.11"},
            {SOPClass.XRayAngiographicImageStorage, "1.2.840.10008.5.1.4.1.1.12.1"},
            {SOPClass.EnhancedXAImageStorage, "1.2.840.10008.5.1.4.1.1.12.1.1"},
            {SOPClass.XRayRadiofluoroscopicImageStorage, "1.2.840.10008.5.1.4.1.1.12.2"},
            {SOPClass.EnhancedXRFImageStorage, "1.2.840.10008.5.1.4.1.1.12.2.1"},
            {SOPClass.XRay3DAngiographicImageStorage, "1.2.840.10008.5.1.4.1.1.13.1.1"},
            {SOPClass.XRay3DCraniofacialImageStorage, "1.2.840.10008.5.1.4.1.1.13.1.2"},
            {SOPClass.BreastTomosynthesisImageStorage, "1.2.840.10008.5.1.4.1.1.13.1.3"},
            {SOPClass.BreastProjectionXRayImageStorageForPresentation, "1.2.840.10008.5.1.4.1.1.13.1.4"},
            {SOPClass.BreastProjectionXRayImageStorageForProcessing, "1.2.840.10008.5.1.4.1.1.13.1.5"},
            {SOPClass.IntravascularOpticalCoherenceTomographyImageStorageForPresentation, "1.2.840.10008.5.1.4.1.1.14.1"},
            {SOPClass.IntravascularOpticalCoherenceTomographyImageStorageForProcessing, "1.2.840.10008.5.1.4.1.1.14.2"},
            {SOPClass.NuclearMedicineImageStorage, "1.2.840.10008.5.1.4.1.1.20"},
            {SOPClass.ParametricMapStorage, "1.2.840.10008.5.1.4.1.1.30"},
            {SOPClass.RawDataStorage, "1.2.840.10008.5.1.4.1.1.66"},
            {SOPClass.SpatialRegistrationStorage, "1.2.840.10008.5.1.4.1.1.66.1"},
            {SOPClass.SpatialFiducialsStorage, "1.2.840.10008.5.1.4.1.1.66.2"},
            {SOPClass.DeformableSpatialRegistrationStorage, "1.2.840.10008.5.1.4.1.1.66.3"},
            {SOPClass.SegmentationStorage, "1.2.840.10008.5.1.4.1.1.66.4"},
            {SOPClass.SurfaceSegmentationStorage, "1.2.840.10008.5.1.4.1.1.66.5"},
            {SOPClass.TractographyResultsStorage, "1.2.840.10008.5.1.4.1.1.66.6"},
            {SOPClass.RealWorldValueMappingStorage, "1.2.840.10008.5.1.4.1.1.67"},
            {SOPClass.SurfaceScanMeshStorage, "1.2.840.10008.5.1.4.1.1.68.1"},
            {SOPClass.SurfaceScanPointCloudStorage, "1.2.840.10008.5.1.4.1.1.68.2"},
            {SOPClass.VLEndoscopicImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.1"},
            {SOPClass.VideoEndoscopicImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.1.1"},
            {SOPClass.VLMicroscopicImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.2"},
            {SOPClass.VideoMicroscopicImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.2.1"},
            {SOPClass.VLSlideCoordinatesMicroscopicImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.3"},
            {SOPClass.VLPhotographicImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.4"},
            {SOPClass.VideoPhotographicImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.4.1"},
            {SOPClass.OphthalmicPhotography8BitImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.1"},
            {SOPClass.OphthalmicPhotography16BitImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.2"},
            {SOPClass.StereometricRelationshipStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.3"},
            {SOPClass.OphthalmicTomographyImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.4"},
            {SOPClass.WideFieldOphthalmicPhotographyStereographicProjectionImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.5"},
            {SOPClass.WideFieldOphthalmicPhotography3DCoordinatesImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.6"},
            {SOPClass.OphthalmicOpticalCoherenceTomographyEnFaceImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.7"},
            {SOPClass.OphthalmicOpticalCoherenceTomographyBscanVolumeAnalysisStorage, "1.2.840.10008.5.1.4.1.1.77.1.5.8"},
            {SOPClass.VLWholeSlideMicroscopyImageStorage, "1.2.840.10008.5.1.4.1.1.77.1.6"},
            {SOPClass.LensometryMeasurementsStorage, "1.2.840.10008.5.1.4.1.1.78.1"},
            {SOPClass.AutorefractionMeasurementsStorage, "1.2.840.10008.5.1.4.1.1.78.2"},
            {SOPClass.KeratometryMeasurementsStorage, "1.2.840.10008.5.1.4.1.1.78.3"},
            {SOPClass.SubjectiveRefractionMeasurementsStorage, "1.2.840.10008.5.1.4.1.1.78.4"},
            {SOPClass.VisualAcuityMeasurementsStorage, "1.2.840.10008.5.1.4.1.1.78.5"},
            {SOPClass.SpectaclePrescriptionReportStorage, "1.2.840.10008.5.1.4.1.1.78.6"},
            {SOPClass.OphthalmicAxialMeasurementsStorage, "1.2.840.10008.5.1.4.1.1.78.7"},
            {SOPClass.IntraocularLensCalculationsStorage, "1.2.840.10008.5.1.4.1.1.78.8"},
            {SOPClass.MacularGridThicknessandVolumeReport, "1.2.840.10008.5.1.4.1.1.79.1"},
            {SOPClass.OphthalmicVisualFieldStaticPerimetryMeasurementsStorage, "1.2.840.10008.5.1.4.1.1.80.1"},
            {SOPClass.OphthalmicThicknessMapStorage, "1.2.840.10008.5.1.4.1.1.81.1"},
            {SOPClass.CornealTopographyMapStorage, "1.2.840.10008.5.1.4.1.1.82.1"},
            {SOPClass.BasicTextSRStorage, "1.2.840.10008.5.1.4.1.1.88.11"},
            {SOPClass.EnhancedSRStorage, "1.2.840.10008.5.1.4.1.1.88.22"},
            {SOPClass.ComprehensiveSRStorage, "1.2.840.10008.5.1.4.1.1.88.33"},
            {SOPClass.Comprehensive3DSRStorage, "1.2.840.10008.5.1.4.1.1.88.34"},
            {SOPClass.ExtensibleSRStorage, "1.2.840.10008.5.1.4.1.1.88.35"},
            {SOPClass.ProcedureLogStorage, "1.2.840.10008.5.1.4.1.1.88.40"},
            {SOPClass.MammographyCADSRStorage, "1.2.840.10008.5.1.4.1.1.88.50"},
            {SOPClass.KeyObjectSelectionStorage, "1.2.840.10008.5.1.4.1.1.88.59"},
            {SOPClass.ChestCADSRStorage, "1.2.840.10008.5.1.4.1.1.88.65"},
            {SOPClass.XRayRadiationDoseSRStorage, "1.2.840.10008.5.1.4.1.1.88.67"},
            {SOPClass.RadiopharmaceuticalRadiationDoseSRStorage, "1.2.840.10008.5.1.4.1.1.88.68"},
            {SOPClass.ColonCADSRStorage, "1.2.840.10008.5.1.4.1.1.88.69"},
            {SOPClass.ImplantationPlanSRDocumentStorage, "1.2.840.10008.5.1.4.1.1.88.70"},
            {SOPClass.AcquisitionContextSRStorage, "1.2.840.10008.5.​1.​4.​1.​1.​88.​71"},
            {SOPClass.SimplifiedAdultEchoSRStorage, "1.2.840.10008.5.​1.​4.​1.​1.​88.​72"},
            {SOPClass.PatientRadiationDoseSRStorage, "1.2.840.10008.5.​1.​4.​1.​1.​88.​73"},
            {SOPClass.ContentAssessmentResultsStorage, "1.2.840.10008.5.1.4.1.1.90.1"},
            {SOPClass.EncapsulatedPDFStorage, "1.2.840.10008.5.1.4.1.1.104.1"},
            {SOPClass.EncapsulatedCDAStorage, "1.2.840.10008.5.1.4.1.1.104.2"},
            {SOPClass.PositronEmissionTomographyImageStorage, "1.2.840.10008.5.1.4.1.1.128"},
            {SOPClass.EnhancedPETImageStorage, "1.2.840.10008.5.1.4.1.1.130"},
            {SOPClass.LegacyConvertedEnhancedPETImageStorage, "1.2.840.10008.5.1.4.1.1.128.1"},
            {SOPClass.BasicStructuredDisplayStorage, "1.2.840.10008.5.1.4.1.1.131"},
            {SOPClass.CTPerformedProcedureProtocolStorage, "1.2.840.10008.5.1.4.1.1.200.2"},
            {SOPClass.RTImageStorage, "1.2.840.10008.5.1.4.1.1.481.1"},
            {SOPClass.RTDoseStorage, "1.2.840.10008.5.1.4.1.1.481.2"},
            {SOPClass.RTStructureSetStorage, "1.2.840.10008.5.1.4.1.1.481.3"},
            {SOPClass.RTBeamsTreatmentRecordStorage, "1.2.840.10008.5.1.4.1.1.481.4"},
            {SOPClass.RTPlanStorage, "1.2.840.10008.5.1.4.1.1.481.5"},
            {SOPClass.RTBrachyTreatmentRecordStorage, "1.2.840.10008.5.1.4.1.1.481.6"},
            {SOPClass.RTTreatmentSummaryRecordStorage, "1.2.840.10008.5.1.4.1.1.481.7"},
            {SOPClass.RTIonPlanStorage, "1.2.840.10008.5.1.4.1.1.481.8"},
            {SOPClass.RTIonBeamsTreatmentRecordStorage, "1.2.840.10008.5.1.4.1.1.481.9"},
            {SOPClass.RTBeamsDeliveryInstructionStorage, "1.2.840.10008.5.1.4.34.7"},
            {SOPClass.RTBrachyApplicationSetupDeliveryInstructionStorage, "1.2.840.10008.5.1.4.34.10"},
        };

        private static readonly Dictionary<string, SOPClass?> _sopCodeMap = new Dictionary<string, SOPClass?>()
        {
            {"1.2.840.10008.5.1.4.1.1.1", SOPClass.ComputedRadiographyImageStorage},
            {"1.2.840.10008.5.1.4.1.1.1.1", SOPClass.DigitalXRayImageStorageForPresentation},
            {"1.2.840.10008.5.1.4.1.1.1.1.1", SOPClass.DigitalXRayImageStorageForProcessing},
            {"1.2.840.10008.5.1.4.1.1.1.2", SOPClass.DigitalMammographyXRayImageStorageForPresentation},
            {"1.2.840.10008.5.1.4.1.1.1.2.1", SOPClass.DigitalMammographyXRayImageStorageForProcessing},
            {"1.2.840.10008.5.1.4.1.1.1.3", SOPClass.DigitalIntraOralXRayImageStorageForPresentation},
            {"1.2.840.10008.5.1.4.1.1.1.3.1", SOPClass.DigitalIntraOralXRayImageStorageForProcessing},
            {"1.2.840.10008.5.1.4.1.1.2", SOPClass.CTImageStorage},
            {"1.2.840.10008.5.1.4.1.1.2.1", SOPClass.EnhancedCTImageStorage},
            {"1.2.840.10008.5.1.4.1.1.2.2", SOPClass.LegacyConvertedEnhancedCTImageStorage},
            {"1.2.840.10008.5.1.4.1.1.3.1", SOPClass.UltrasoundMultiframeImageStorage},
            {"1.2.840.10008.5.1.4.1.1.4", SOPClass.MRImageStorage},
            {"1.2.840.10008.5.1.4.1.1.4.1", SOPClass.EnhancedMRImageStorage},
            {"1.2.840.10008.5.1.4.1.1.4.2", SOPClass.MRSpectroscopyStorage},
            {"1.2.840.10008.5.1.4.1.1.4.3", SOPClass.EnhancedMRColorImageStorage},
            {"1.2.840.10008.5.1.4.1.1.4.4", SOPClass.LegacyConvertedEnhancedMRImageStorage},
            {"1.2.840.10008.5.1.4.1.1.6.1", SOPClass.UltrasoundImageStorage},
            {"1.2.840.10008.5.1.4.1.1.6.2", SOPClass.EnhancedUSVolumeStorage},
            {"1.2.840.10008.5.1.4.1.1.7", SOPClass.SecondaryCaptureImageStorage},
            {"1.2.840.10008.5.1.4.1.1.7.1", SOPClass.MultiframeSingleBitSecondaryCaptureImageStorage},
            {"1.2.840.10008.5.1.4.1.1.7.2", SOPClass.MultiframeGrayscaleByteSecondaryCaptureImageStorage},
            {"1.2.840.10008.5.1.4.1.1.7.3", SOPClass.MultiframeGrayscaleWordSecondaryCaptureImageStorage},
            {"1.2.840.10008.5.1.4.1.1.7.4", SOPClass.MultiframeTrueColorSecondaryCaptureImageStorage},
            {"1.2.840.10008.5.1.4.1.1.9.1.1", SOPClass._12leadECGWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.1.2", SOPClass.GeneralECGWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.1.3", SOPClass.AmbulatoryECGWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.2.1", SOPClass.HemodynamicWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.3.1", SOPClass.CardiacElectrophysiologyWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.4.1", SOPClass.BasicVoiceAudioWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.4.2", SOPClass.GeneralAudioWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.5.1", SOPClass.ArterialPulseWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.9.6.1", SOPClass.RespiratoryWaveformStorage},
            {"1.2.840.10008.5.1.4.1.1.11.1", SOPClass.GrayscaleSoftcopyPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.2", SOPClass.ColorSoftcopyPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.3", SOPClass.PseudoColorSoftcopyPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.4", SOPClass.BlendingSoftcopyPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.5", SOPClass.XAXRFGrayscaleSoftcopyPresentationStateStorage},
            {"1.2.840.10008.​5.​1.​4.​1.​1.​11.​6", SOPClass.GrayscalePlanarMPRVolumetricPresentationStateStorage},
            {"1.2.840.10008.​5.​1.​4.​1.​1.​11.​7", SOPClass.CompositingPlanarMPRVolumetricPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.8", SOPClass.AdvancedBlendingPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.9", SOPClass.VolumeRenderingVolumetricPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.10", SOPClass.SegmentedVolumeRenderingVolumetricPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.11.11", SOPClass.MultipleVolumeRenderingVolumetricPresentationStateStorage},
            {"1.2.840.10008.5.1.4.1.1.12.1", SOPClass.XRayAngiographicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.12.1.1", SOPClass.EnhancedXAImageStorage},
            {"1.2.840.10008.5.1.4.1.1.12.2", SOPClass.XRayRadiofluoroscopicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.12.2.1", SOPClass.EnhancedXRFImageStorage},
            {"1.2.840.10008.5.1.4.1.1.13.1.1", SOPClass.XRay3DAngiographicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.13.1.2", SOPClass.XRay3DCraniofacialImageStorage},
            {"1.2.840.10008.5.1.4.1.1.13.1.3", SOPClass.BreastTomosynthesisImageStorage},
            {"1.2.840.10008.5.1.4.1.1.13.1.4", SOPClass.BreastProjectionXRayImageStorageForPresentation},
            {"1.2.840.10008.5.1.4.1.1.13.1.5", SOPClass.BreastProjectionXRayImageStorageForProcessing},
            {"1.2.840.10008.5.1.4.1.1.14.1", SOPClass.IntravascularOpticalCoherenceTomographyImageStorageForPresentation},
            {"1.2.840.10008.5.1.4.1.1.14.2", SOPClass.IntravascularOpticalCoherenceTomographyImageStorageForProcessing},
            {"1.2.840.10008.5.1.4.1.1.20", SOPClass.NuclearMedicineImageStorage},
            {"1.2.840.10008.5.1.4.1.1.30", SOPClass.ParametricMapStorage},
            {"1.2.840.10008.5.1.4.1.1.66", SOPClass.RawDataStorage},
            {"1.2.840.10008.5.1.4.1.1.66.1", SOPClass.SpatialRegistrationStorage},
            {"1.2.840.10008.5.1.4.1.1.66.2", SOPClass.SpatialFiducialsStorage},
            {"1.2.840.10008.5.1.4.1.1.66.3", SOPClass.DeformableSpatialRegistrationStorage},
            {"1.2.840.10008.5.1.4.1.1.66.4", SOPClass.SegmentationStorage},
            {"1.2.840.10008.5.1.4.1.1.66.5", SOPClass.SurfaceSegmentationStorage},
            {"1.2.840.10008.5.1.4.1.1.66.6", SOPClass.TractographyResultsStorage},
            {"1.2.840.10008.5.1.4.1.1.67", SOPClass.RealWorldValueMappingStorage},
            {"1.2.840.10008.5.1.4.1.1.68.1", SOPClass.SurfaceScanMeshStorage},
            {"1.2.840.10008.5.1.4.1.1.68.2", SOPClass.SurfaceScanPointCloudStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.1", SOPClass.VLEndoscopicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.1.1", SOPClass.VideoEndoscopicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.2", SOPClass.VLMicroscopicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.2.1", SOPClass.VideoMicroscopicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.3", SOPClass.VLSlideCoordinatesMicroscopicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.4", SOPClass.VLPhotographicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.4.1", SOPClass.VideoPhotographicImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.1", SOPClass.OphthalmicPhotography8BitImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.2", SOPClass.OphthalmicPhotography16BitImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.3", SOPClass.StereometricRelationshipStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.4", SOPClass.OphthalmicTomographyImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.5", SOPClass.WideFieldOphthalmicPhotographyStereographicProjectionImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.6", SOPClass.WideFieldOphthalmicPhotography3DCoordinatesImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.7", SOPClass.OphthalmicOpticalCoherenceTomographyEnFaceImageStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.5.8", SOPClass.OphthalmicOpticalCoherenceTomographyBscanVolumeAnalysisStorage},
            {"1.2.840.10008.5.1.4.1.1.77.1.6", SOPClass.VLWholeSlideMicroscopyImageStorage},
            {"1.2.840.10008.5.1.4.1.1.78.1", SOPClass.LensometryMeasurementsStorage},
            {"1.2.840.10008.5.1.4.1.1.78.2", SOPClass.AutorefractionMeasurementsStorage},
            {"1.2.840.10008.5.1.4.1.1.78.3", SOPClass.KeratometryMeasurementsStorage},
            {"1.2.840.10008.5.1.4.1.1.78.4", SOPClass.SubjectiveRefractionMeasurementsStorage},
            {"1.2.840.10008.5.1.4.1.1.78.5", SOPClass.VisualAcuityMeasurementsStorage},
            {"1.2.840.10008.5.1.4.1.1.78.6", SOPClass.SpectaclePrescriptionReportStorage},
            {"1.2.840.10008.5.1.4.1.1.78.7", SOPClass.OphthalmicAxialMeasurementsStorage},
            {"1.2.840.10008.5.1.4.1.1.78.8", SOPClass.IntraocularLensCalculationsStorage},
            {"1.2.840.10008.5.1.4.1.1.79.1", SOPClass.MacularGridThicknessandVolumeReport},
            {"1.2.840.10008.5.1.4.1.1.80.1", SOPClass.OphthalmicVisualFieldStaticPerimetryMeasurementsStorage},
            {"1.2.840.10008.5.1.4.1.1.81.1", SOPClass.OphthalmicThicknessMapStorage},
            {"1.2.840.10008.5.1.4.1.1.82.1", SOPClass.CornealTopographyMapStorage},
            {"1.2.840.10008.5.1.4.1.1.88.11", SOPClass.BasicTextSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.22", SOPClass.EnhancedSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.33", SOPClass.ComprehensiveSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.34", SOPClass.Comprehensive3DSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.35", SOPClass.ExtensibleSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.40", SOPClass.ProcedureLogStorage},
            {"1.2.840.10008.5.1.4.1.1.88.50", SOPClass.MammographyCADSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.59", SOPClass.KeyObjectSelectionStorage},
            {"1.2.840.10008.5.1.4.1.1.88.65", SOPClass.ChestCADSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.67", SOPClass.XRayRadiationDoseSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.68", SOPClass.RadiopharmaceuticalRadiationDoseSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.69", SOPClass.ColonCADSRStorage},
            {"1.2.840.10008.5.1.4.1.1.88.70", SOPClass.ImplantationPlanSRDocumentStorage},
            {"1.2.840.10008.5.​1.​4.​1.​1.​88.​71", SOPClass.AcquisitionContextSRStorage},
            {"1.2.840.10008.5.​1.​4.​1.​1.​88.​72", SOPClass.SimplifiedAdultEchoSRStorage},
            {"1.2.840.10008.5.​1.​4.​1.​1.​88.​73", SOPClass.PatientRadiationDoseSRStorage},
            {"1.2.840.10008.5.1.4.1.1.90.1", SOPClass.ContentAssessmentResultsStorage},
            {"1.2.840.10008.5.1.4.1.1.104.1", SOPClass.EncapsulatedPDFStorage},
            {"1.2.840.10008.5.1.4.1.1.104.2", SOPClass.EncapsulatedCDAStorage},
            {"1.2.840.10008.5.1.4.1.1.128", SOPClass.PositronEmissionTomographyImageStorage},
            {"1.2.840.10008.5.1.4.1.1.130", SOPClass.EnhancedPETImageStorage},
            {"1.2.840.10008.5.1.4.1.1.128.1", SOPClass.LegacyConvertedEnhancedPETImageStorage},
            {"1.2.840.10008.5.1.4.1.1.131", SOPClass.BasicStructuredDisplayStorage},
            {"1.2.840.10008.5.1.4.1.1.200.2", SOPClass.CTPerformedProcedureProtocolStorage},
            {"1.2.840.10008.5.1.4.1.1.481.1", SOPClass.RTImageStorage},
            {"1.2.840.10008.5.1.4.1.1.481.2", SOPClass.RTDoseStorage},
            {"1.2.840.10008.5.1.4.1.1.481.3", SOPClass.RTStructureSetStorage},
            {"1.2.840.10008.5.1.4.1.1.481.4", SOPClass.RTBeamsTreatmentRecordStorage},
            {"1.2.840.10008.5.1.4.1.1.481.5", SOPClass.RTPlanStorage},
            {"1.2.840.10008.5.1.4.1.1.481.6", SOPClass.RTBrachyTreatmentRecordStorage},
            {"1.2.840.10008.5.1.4.1.1.481.7", SOPClass.RTTreatmentSummaryRecordStorage},
            {"1.2.840.10008.5.1.4.1.1.481.8", SOPClass.RTIonPlanStorage},
            {"1.2.840.10008.5.1.4.1.1.481.9", SOPClass.RTIonBeamsTreatmentRecordStorage},
            {"1.2.840.10008.5.1.4.34.7", SOPClass.RTBeamsDeliveryInstructionStorage},
            {"1.2.840.10008.5.1.4.34.10", SOPClass.RTBrachyApplicationSetupDeliveryInstructionStorage},
        };

        #endregion

        public static SOPClass? SOPClassName(string code)
        {
            _sopCodeMap.TryGetValue(code, out var v);
            return v;
        }

        public static string SOPClassCode(SOPClass sop)
        {
            _sopNameMap.TryGetValue(sop, out var v);
            return v;
        }
    }
}
