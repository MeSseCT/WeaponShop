#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
project_dir="${repo_root}/WeaponShop.web"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Chyba: príkaz 'dotnet' nie je dostupný v PATH."
    exit 1
fi

if [[ ! -f "${project_dir}/WeaponShop.Web.csproj" ]]; then
    echo "Chyba: nenašiel sa projekt ${project_dir}/WeaponShop.Web.csproj."
    exit 1
fi

prompt_required() {
    local label="$1"
    local value=""

    while [[ -z "${value}" ]]; do
        read -r -p "${label}" value
        if [[ -z "${value}" ]]; then
            echo "Táto hodnota je povinná."
        fi
    done

    printf '%s' "${value}"
}

prompt_optional() {
    local label="$1"
    local value=""
    read -r -p "${label}" value
    printf '%s' "${value}"
}

prompt_optional_secret() {
    local label="$1"
    local value=""
    read -r -s -p "${label}" value
    echo
    printf '%s' "${value}"
}

set_or_remove_secret() {
    local key="$1"
    local value="$2"

    if [[ -n "${value}" ]]; then
        dotnet user-secrets --project "${project_dir}" set "${key}" "${value}" >/dev/null
    else
        dotnet user-secrets --project "${project_dir}" remove "${key}" >/dev/null || true
    fi
}

echo "Nastavenie lokálnych user-secrets pre WeaponShop"
echo "Prázdna voliteľná hodnota existujúci secret odstráni."
echo

connection_string="$(prompt_required "Connection string pre DefaultConnection: ")"
admin_password="$(prompt_optional_secret "Seed heslo pre admina (prázdne = seed admin sa nevytvorí): ")"
warehouse_password="$(prompt_optional_secret "Seed heslo pre skladníka (prázdne = seed skladník sa nevytvorí): ")"
gunsmith_password="$(prompt_optional_secret "Seed heslo pre zbrojíra (prázdne = seed zbrojíř sa nevytvorí): ")"
smtp_user="$(prompt_optional "SMTP používateľ (prázdne = odstrániť): ")"
smtp_password="$(prompt_optional_secret "SMTP heslo (prázdne = odstrániť): ")"

set_or_remove_secret "ConnectionStrings:DefaultConnection" "${connection_string}"
set_or_remove_secret "SeedAdmin:Password" "${admin_password}"
set_or_remove_secret "SeedWarehouse:Password" "${warehouse_password}"
set_or_remove_secret "SeedGunsmith:Password" "${gunsmith_password}"
set_or_remove_secret "Email:Smtp:User" "${smtp_user}"
set_or_remove_secret "Email:Smtp:Password" "${smtp_password}"

echo
echo "Hotovo."
echo "Spusti aplikáciu cez: dotnet run --project ${project_dir}/WeaponShop.Web.csproj"
