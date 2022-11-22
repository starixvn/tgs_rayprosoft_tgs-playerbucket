{{/*
Expand the name of the chart.
*/}}
{{- define "playerbucket-migrate-job.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "playerbucket-migrate-job.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "playerbucket-migrate-job.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "playerbucket-migrate-job.labels" -}}
helm.sh/chart: {{ include "playerbucket-migrate-job.chart" . }}
{{ include "playerbucket-migrate-job.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "playerbucket-migrate-job.selectorLabels" -}}
app.kubernetes.io/name: {{ include "playerbucket-migrate-job.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
tgs.kind: payment
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "playerbucket-migrate-job.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "playerbucket-migrate-job.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}
